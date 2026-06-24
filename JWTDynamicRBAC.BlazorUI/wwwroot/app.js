// =============================================
// JWT RBAC — Cookie Helpers (called from Blazor via IJSRuntime)
// =============================================

/**
 * Set the authToken cookie safely — avoids eval() quote escaping issues
 * Called from Login.razor: JS.InvokeVoidAsync("setCookieToken", token)
 */
window.setCookieToken = function (token) {
    var expires = new Date(Date.now() + 2 * 60 * 60 * 1000).toUTCString();
    document.cookie = 'authToken=' + encodeURIComponent(token)
        + '; expires=' + expires
        + '; path=/; SameSite=Strict';
};

/**
 * Remove the authToken cookie
 */
window.removeCookieToken = function () {
    document.cookie = 'authToken=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/; SameSite=Strict';
};

/**
 * Read the authToken cookie value
 */
window.getCookieToken = function () {
    var match = document.cookie.match(/(?:^|; )authToken=([^;]*)/);
    return match ? decodeURIComponent(match[1]) : null;
};
