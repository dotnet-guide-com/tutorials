// passkeys.js — WebAuthn JavaScript for the passkey identity sample

const statusEl = document.getElementById('status');
const unauthSection = document.getElementById('unauthenticated-section');
const authSection = document.getElementById('authenticated-section');
const passkeySigninSection = document.getElementById('passkey-signin-section');

let antiforgeryToken = null;

function showStatus(message, type) {
  statusEl.className = type;
  statusEl.textContent = message;
}

function showError(message) {
  showStatus(message, 'error');
}

function showSuccess(message) {
  showStatus(message, 'success');
}

function showInfo(message) {
  showStatus(message, 'info');
}

// UI state
function updateAuthUI(isAuthenticated) {
  if (isAuthenticated) {
    unauthSection.classList.add('hidden');
    authSection.classList.remove('hidden');
  } else {
    unauthSection.classList.remove('hidden');
    authSection.classList.add('hidden');
  }
}

// Antiforgery token
async function fetchAntiforgeryToken() {
  const response = await fetch('/antiforgery/token');
  if (!response.ok) throw new Error('Failed to get antiforgery token');
  const data = await response.json();
  antiforgeryToken = data.token;
  return antiforgeryToken;
}

// Generic POST helper
async function apiPost(url, body, requiresAuth = false) {
  const headers = { 'Content-Type': 'application/json' };
  if (antiforgeryToken) {
    headers['RequestVerificationToken'] = antiforgeryToken;
  }
  const response = await fetch(url, {
    method: 'POST',
    headers,
    credentials: 'same-origin',
    body: body ? JSON.stringify(body) : undefined
  });
  return response;
}

// Bootstrap login
async function bootstrapLogin() {
  const email = document.getElementById('email').value;
  const password = document.getElementById('password').value;

  showInfo('Signing in with password...');

  try {
    await fetchAntiforgeryToken();
    const response = await apiPost('/account/login', { email, password });

    if (response.status === 204) {
      showSuccess('Signed in with password. You can now create a passkey.');
      updateAuthUI(true);
    } else {
      showError('Login failed. Check credentials.');
    }
  } catch (err) {
    showError('Login error: ' + err.message);
  }
}

// Logout
async function logout() {
  showInfo('Signing out...');

  try {
    await fetchAntiforgeryToken();
    const response = await apiPost('/account/logout');

    if (response.status === 204) {
      showSuccess('Signed out.');
      updateAuthUI(false);
      antiforgeryToken = null;
    } else {
      showError('Logout failed.');
    }
  } catch (err) {
    showError('Logout error: ' + err.message);
  }
}

// Create passkey
async function createPasskey() {
  showInfo('Requesting passkey creation options...');

  try {
    await fetchAntiforgeryToken();

    // Step 1: Get creation options from server
    const optionsResponse = await apiPost('/account/passkeys/creation-options');

    if (!optionsResponse.ok) {
      showError('Failed to get creation options (status ' + optionsResponse.status + ').');
      return;
    }

    const creationOptionsJson = await optionsResponse.text();

    // Step 2: Parse creation options
    let publicKeyCredentialCreationOptions;
    try {
      publicKeyCredentialCreationOptions =
        PublicKeyCredential.parseCreationOptionsFromJSON(creationOptionsJson);
    } catch (e) {
      showError('Failed to parse creation options: ' + e.message);
      return;
    }

    showInfo('Waiting for authenticator interaction...');

    // Step 3: Call browser WebAuthn API
    const credential = await navigator.credentials.create({
      publicKey: publicKeyCredentialCreationOptions
    });

    // Step 4: Serialize credential to JSON
    const credentialJson = JSON.stringify(credential.toJSON());

    // Step 5: Send credential to server
    await fetchAntiforgeryToken();
    const registerResponse = await apiPost('/account/passkeys/register', {
      credentialJson: credentialJson
    });

    if (registerResponse.status === 204) {
      showSuccess('Passkey created and registered successfully!');
    } else {
      const errBody = await registerResponse.text();
      showError('Registration failed (status ' + registerResponse.status + '): ' + errBody);
    }
  } catch (err) {
    if (err.name === 'NotAllowedError') {
      showError('Authenticator interaction was cancelled.');
    } else if (err.name === 'NotSupportedError') {
      showError('Passkeys are not supported in this browser or context (requires HTTPS or localhost).');
    } else {
      showError('Passkey creation error: ' + err.message);
    }
  }
}

// List passkeys
async function listPasskeys() {
  showInfo('Fetching passkeys...');

  try {
    const response = await fetch('/account/passkeys', {
      credentials: 'same-origin'
    });

    if (response.status === 401) {
      showError('Not authenticated.');
      updateAuthUI(false);
      return;
    }

    const passkeys = await response.json();
    if (passkeys.length === 0) {
      showInfo('No passkeys registered yet.');
    } else {
      let message = 'Passkeys:\n';
      for (const pk of passkeys) {
        message += `  - ${pk.name || '(unnamed)'} (ID: ${pk.credentialId.substring(0, 20)}...)\n`;
        message += `    Backup eligible: ${pk.isBackupEligible}, Backed up: ${pk.isBackedUp}\n`;
      }
      showSuccess(message);
    }
  } catch (err) {
    showError('List passkeys error: ' + err.message);
  }
}

// Passkey sign-in
async function passkeySignIn() {
  const email = document.getElementById('passkey-email').value;

  showInfo('Requesting passkey request options...');

  try {
    await fetchAntiforgeryToken();

    // Step 1: Get request options
    const optionsResponse = await apiPost('/account/passkeys/request-options', {
      email: email
    });

    if (!optionsResponse.ok) {
      showError('Failed to get request options (status ' + optionsResponse.status + ').');
      return;
    }

    const requestOptionsJson = await optionsResponse.text();

    // If empty response, no passkeys found for this user
    if (!requestOptionsJson || requestOptionsJson === '{}') {
      showError('No passkeys found for this account. Enroll a passkey first.');
      return;
    }

    // Step 2: Parse request options
    let publicKeyCredentialRequestOptions;
    try {
      publicKeyCredentialRequestOptions =
        PublicKeyCredential.parseRequestOptionsFromJSON(requestOptionsJson);
    } catch (e) {
      showError('Failed to parse request options: ' + e.message);
      return;
    }

    showInfo('Waiting for authenticator interaction...');

    // Step 3: Call browser WebAuthn API
    const assertion = await navigator.credentials.get({
      publicKey: publicKeyCredentialRequestOptions
    });

    // Step 4: Serialize assertion to JSON
    const assertionJson = JSON.stringify(assertion.toJSON());

    // Step 5: Send assertion to server
    await fetchAntiforgeryToken();
    const signInResponse = await apiPost('/account/passkeys/sign-in', {
      credentialJson: assertionJson
    });

    if (signInResponse.status === 204) {
      showSuccess('Signed in with passkey!');
      updateAuthUI(true);
    } else {
      showError('Passkey sign-in failed (status ' + signInResponse.status + ').');
    }
  } catch (err) {
    if (err.name === 'NotAllowedError') {
      showError('Authenticator interaction was cancelled.');
    } else if (err.name === 'NotSupportedError') {
      showError('Passkeys are not supported in this browser or context (requires HTTPS or localhost).');
    } else {
      showError('Passkey sign-in error: ' + err.message);
    }
  }
}

// Wire up buttons
document.getElementById('btn-login').addEventListener('click', bootstrapLogin);
document.getElementById('btn-logout').addEventListener('click', logout);
document.getElementById('btn-create-passkey').addEventListener('click', createPasskey);
document.getElementById('btn-list-passkeys').addEventListener('click', listPasskeys);
document.getElementById('btn-passkey-signin').addEventListener('click', passkeySignIn);

// Check if WebAuthn is available
if (!window.PublicKeyCredential) {
  showError('WebAuthn is not supported in this browser. Use a modern browser with HTTPS or localhost.');
}

// Initial state
updateAuthUI(false);
showInfo('Ready. Sign in with the bootstrap password to get started.');