﻿//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Requests;
using System.Collections.Generic;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Instance;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.TelemetryCore;
using System.Threading;
using Microsoft.Identity.Client.Config;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Features.DeviceCode;
using Microsoft.Identity.Client.WsTrust;

namespace Microsoft.Identity.Client
{
#if !DESKTOP && !NET_CORE
#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
#endif
    /// <summary>
    /// Class to be used to acquire tokens in desktop or mobile applications (Desktop / UWP / Xamarin.iOS / Xamarin.Android).
    /// public client applications are not trusted to safely keep application secrets, and therefore they only access Web APIs in the name of the user only
    /// (they only support public client flows). For details see https://aka.ms/msal-net-client-applications
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Contrary to <see cref="Microsoft.Identity.Client.ConfidentialClientApplication"/>, public clients are unable to hold configuration time secrets,
    /// and as a result have no client secret</description></item>
    /// <item><description>the redirect URL is pre-proposed by the library. It does not need to be passed in the constructor</description></item>
    /// <item><description>.NET Core does not support UI, and therefore this platform does not provide the interactive token acquisition methods</description></item>
    /// </list>
    /// </remarks>
    public sealed partial class PublicClientApplication : ClientApplicationBase, IPublicClientApplication
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
    {
        internal PublicClientApplication(ApplicationConfiguration configuration) : base(configuration)
        {

        }

        /// <summary>
        /// Consutructor of the application. It will use https://login.microsoftonline.com/common as the default authority.
        /// </summary>
        /// <param name="clientId">Client ID (also known as App ID) of the application as registered in the
        /// application registration portal (https://aka.ms/msal-net-register-app)/. REQUIRED</param>
        public PublicClientApplication(string clientId) : this(clientId, DefaultAuthority)
        {
        }

        /// <summary>
        /// Consutructor of the application.
        /// </summary>
        /// <param name="clientId">Client ID (also named Application ID) of the application as registered in the
        /// application registration portal (https://aka.ms/msal-net-register-app)/. REQUIRED</param>
        /// <param name="authority">Authority of the security token service (STS) from which MSAL.NET will acquire the tokens.
        /// Usual authorities are:
        /// <list type="bullet">
        /// <item><description><c>https://login.microsoftonline.com/tenant/</c>, where <c>tenant</c> is the tenant ID of the Azure AD tenant
        /// or a domain associated with this Azure AD tenant, in order to sign-in user of a specific organization only</description></item>
        /// <item><description><c>https://login.microsoftonline.com/common/</c> to signing users with any work and school accounts or Microsoft personal account</description></item>
        /// <item><description><c>https://login.microsoftonline.com/organizations/</c> to signing users with any work and school accounts</description></item>
        /// <item><description><c>https://login.microsoftonline.com/consumers/</c> to signing users with only personal Microsoft account (live)</description></item>
        /// </list>
        /// Note that this setting needs to be consistent with what is declared in the application registration portal
        /// </param>
        public PublicClientApplication(string clientId, string authority)
            : base(PublicClientApplicationBuilder
                   .Create(
                       clientId, 
                       "WHATTENANTGOESHERE", 
                       PlatformProxyFactory.GetPlatformProxy().GetDefaultRedirectUri(clientId))
                   .WithAuthority(authority, true, true)
                   .WithUserTokenCache(new TokenCache())  // TODO: ensure clientid gets set properly this way during build...
                   .BuildConfiguration())
        {
        }

        //internal PublicClientApplication(IServiceBundle serviceBundle, string clientId, string authority)
        //    : base(
        //        clientId,
        //        authority,
        //        PlatformProxyFactory.GetPlatformProxy().GetDefaultRedirectUri(clientId),
        //        true,
        //        serviceBundle)
        //{
        //    UserTokenCache = new TokenCache()
        //    {
        //        ClientId = clientId
        //    };
        //}

        // netcoreapp does not support UI at the moment and all the Acquire* methods use UI;
        // however include the signatures at runtime only to prevent MissingMethodExceptions from NetStandard
#if !NET_CORE_BUILDTIME // include for other platforms and for runtime

#if iOS
        private string keychainSecurityGroup;

        /// <summary>
        /// Xamarin iOS specific property enabling the application to share the token cache with other applications sharing the same keychain security group.
        /// If you provide this key, you MUST add the capability to your Application Entitlement.
        /// For more details, please see https://aka.ms/msal-net-sharing-cache-on-ios
        /// </summary>
        /// <remarks>This API may change in future release.</remarks>
        public string KeychainSecurityGroup
        {
            get
            {
                return keychainSecurityGroup;
            }
            set
            {
                keychainSecurityGroup = value;
                UserTokenCache.TokenCacheAccessor.SetKeychainSecurityGroup(value);
                UserTokenCache.LegacyCachePersistence.SetKeychainSecurityGroup(value);
            }
        }
#endif

#if WINDOWS_APP
        /// <summary>
        /// Flag to enable authentication with the user currently logged-in in Windows.
        /// When set to true, the application will try to connect to the corporate network using windows integrated authentication.
        /// </summary>
        public bool UseCorporateNetwork { get; set; }
#endif

        // Android does not support AcquireToken* without UIParent params, but include it at runtime
        // only to avoid MissingMethodExceptions from NetStandard
#if !ANDROID_BUILDTIME // include for other other platform and for runtime
        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The user is required to select an account
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        /// <remarks>The user will be signed-in interactively if needed,
        /// and will consent to scopes and do multi-factor authentication if such a policy was enabled in the Azure AD tenant.</remarks>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes)
        {
            GuardNetCore();
            GuardUIParentAndroid();

            Authority authority = Instance.Authority.CreateAuthority(ServiceBundle);
            return
                await
                    AcquireTokenForLoginHintCommonAsync(authority, scopes, null, null,
                        UIBehavior.SelectAccount, null, null, ApiEvent.ApiIds.AcquireTokenWithScope).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The user will need to sign-in but an account will be proposed
        /// based on the <paramref name="loginHint"/>
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="loginHint">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, string loginHint)
        {
            GuardNetCore();
            GuardUIParentAndroid();

            Authority authority = Instance.Authority.CreateAuthority(ServiceBundle);
            return
                await
                    AcquireTokenForLoginHintCommonAsync(authority, scopes, null, loginHint,
                        UIBehavior.SelectAccount, null, null, ApiEvent.ApiIds.AcquireTokenWithScopeHint).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The user will need to sign-in but an account will be proposed
        /// based on the provided <paramref name="account"/>
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account to use for the interactive token acquisition. See <see cref="IAccount"/> for ways to get an account</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            IAccount account)
        {
            GuardNetCore();
            GuardUIParentAndroid();

            Authority authority = Instance.Authority.CreateAuthority(ServiceBundle);
            return
                await
                    AcquireTokenForUserCommonAsync(authority, scopes, null, account,
                        UIBehavior.SelectAccount, null, null, ApiEvent.ApiIds.AcquireTokenWithScopeUser).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token for a login with control of the UI behavior and possiblity of passing extra query parameters like additional claims
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="loginHint">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="behavior">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, string loginHint,
            UIBehavior behavior, string extraQueryParameters)
        {
            GuardNetCore();
            GuardUIParentAndroid();

            Authority authority = Instance.Authority.CreateAuthority(ServiceBundle);
            return
                await
                    AcquireTokenForLoginHintCommonAsync(authority, scopes, null, loginHint,
                        behavior, extraQueryParameters, null, ApiEvent.ApiIds.AcquireTokenWithScopeHintBehavior).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token for an account with control of the UI behavior and possiblity of passing extra query parameters like additional claims
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account to use for the interactive token acquisition. See <see cref="IAccount"/> for ways to get an account</param>
        /// <param name="behavior">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, IAccount account,
            UIBehavior behavior, string extraQueryParameters)
        {
            GuardNetCore();
            GuardUIParentAndroid();

            Authority authority = Instance.Authority.CreateAuthority(ServiceBundle);
            return
                await
                    AcquireTokenForUserCommonAsync(authority, scopes, null, account, behavior,
                        extraQueryParameters, null, ApiEvent.ApiIds.AcquireTokenWithScopeUserBehavior).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token for a given login, with the possibility of controlling the user experience, passing extra query
        /// parameters, providing extra scopes that the user can pre-consent to, and overriding the authority pre-configured in the application
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="loginHint">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="behavior">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="extraScopesToConsent">Scopes that you can request the end user to consent upfront, in addition to the scopes for the protected Web API
        /// for which you want to acquire a security token.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, string loginHint,
            UIBehavior behavior, string extraQueryParameters, IEnumerable<string> extraScopesToConsent, string authority)
        {
            GuardNetCore();
            GuardUIParentAndroid();

            Authority authorityInstance = Instance.Authority.CreateAuthority(ServiceBundle);
            return
                await
                    AcquireTokenForLoginHintCommonAsync(authorityInstance, scopes, extraScopesToConsent,
                        loginHint, behavior, extraQueryParameters, null, ApiEvent.ApiIds.AcquireTokenWithScopeHintBehaviorAuthority).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token for a given account, with the possibility of controlling the user experience, passing extra query
        /// parameters, providing extra scopes that the user can pre-consent to, and overriding the authority pre-configured in the application
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account to use for the interactive token acquisition. See <see cref="IAccount"/> for ways to get an account</param>
        /// <param name="behavior">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="extraScopesToConsent">Scopes that you can request the end user to consent upfront, in addition to the scopes for the protected Web API
        /// for which you want to acquire a security token.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, IAccount account,
            UIBehavior behavior, string extraQueryParameters, IEnumerable<string> extraScopesToConsent, string authority)
        {
            GuardNetCore();
            GuardUIParentAndroid();

            Authority authorityInstance = Instance.Authority.CreateAuthorityWithOverride(
                ServiceBundle, 
                AuthorityInfo.FromAuthorityUri(
                    authority,
                    ServiceBundle.Config.DefaultAuthorityInfo.ValidateAuthority,
                    false));
            return
                await
                    AcquireTokenForUserCommonAsync(authorityInstance, scopes, extraScopesToConsent, account,
                        behavior, extraQueryParameters, null, ApiEvent.ApiIds.AcquireTokenWithScopeUserBehaviorAuthority).ConfigureAwait(false);
        }
#endif

        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The interactive window will be parented to the specified
        /// window. The user will be required to select an account
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        /// <remarks>The user will be signed-in interactively if needed,
        /// and will consent to scopes and do multi-factor authentication if such a policy was enabled in the Azure AD tenant.</remarks>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, UIParent parent)
        {
            GuardNetCore();

            Authority authority = Instance.Authority.CreateAuthority(ServiceBundle);
            return
                await
                    AcquireTokenForLoginHintCommonAsync(authority, scopes, null, null,
                        UIBehavior.SelectAccount, null, parent, ApiEvent.ApiIds.AcquireTokenWithScope).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The interactive window will be parented to the specified
        /// window. The user will need to sign-in but an account will be proposed
        /// based on the <paramref name="loginHint"/>
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="loginHint">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and login</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, string loginHint, UIParent parent)
        {
            GuardNetCore();

            Authority authority = Instance.Authority.CreateAuthority(ServiceBundle);
            return
                await
                    AcquireTokenForLoginHintCommonAsync(authority, scopes, null, loginHint,
                        UIBehavior.SelectAccount, null, parent, ApiEvent.ApiIds.AcquireTokenWithScopeHint).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token for the specified scopes. The user will need to sign-in but an account will be proposed
        /// based on the provided <paramref name="account"/>
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account to use for the interactive token acquisition. See <see cref="IAccount"/> for ways to get an account</param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(
            IEnumerable<string> scopes,
            IAccount account, UIParent parent)
        {
            GuardNetCore();

            Authority authority = Instance.Authority.CreateAuthority(ServiceBundle);
            return
                await
                    AcquireTokenForUserCommonAsync(authority, scopes, null, account,
                        UIBehavior.SelectAccount, null, parent, ApiEvent.ApiIds.AcquireTokenWithScopeUser).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token for a login with control of the UI behavior and possiblity of passing extra query parameters like additional claims
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="loginHint">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="behavior">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, string loginHint,
            UIBehavior behavior, string extraQueryParameters, UIParent parent)
        {
            GuardNetCore();

            Authority authority = Instance.Authority.CreateAuthority(ServiceBundle);
            return
                await
                    AcquireTokenForLoginHintCommonAsync(authority, scopes, null, loginHint,
                        behavior, extraQueryParameters, parent, ApiEvent.ApiIds.AcquireTokenWithScopeHintBehavior).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token for an account with control of the UI behavior and possiblity of passing extra query parameters like additional claims
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account to use for the interactive token acquisition. See <see cref="IAccount"/> for ways to get an account</param>
        /// <param name="behavior">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, IAccount account,
            UIBehavior behavior, string extraQueryParameters, UIParent parent)
        {
            GuardNetCore();

            Authority authority = Instance.Authority.CreateAuthority(ServiceBundle);
            return
                await
                    AcquireTokenForUserCommonAsync(authority, scopes, null, account, behavior,
                        extraQueryParameters, parent, ApiEvent.ApiIds.AcquireTokenWithScopeUserBehavior).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token for a given login, with the possibility of controlling the user experience, passing extra query
        /// parameters, providing extra scopes that the user can pre-consent to, and overriding the authority pre-configured in the application
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="loginHint">Identifier of the user. Generally in UserPrincipalName (UPN) format, e.g. <c>john.doe@contoso.com</c></param>
        /// <param name="behavior">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="extraScopesToConsent">scopes that you can request the end user to consent upfront, in addition to the scopes for the protected Web API
        /// for which you want to acquire a security token.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, string loginHint,
            UIBehavior behavior, string extraQueryParameters, IEnumerable<string> extraScopesToConsent, string authority, UIParent parent)
        {
            GuardNetCore();

            Authority authorityInstance = Instance.Authority.CreateAuthorityWithOverride(
                ServiceBundle, 
                AuthorityInfo.FromAuthorityUri(
                    authority,
                    ServiceBundle.Config.DefaultAuthorityInfo.ValidateAuthority,
                    false));

            return
                await
                    AcquireTokenForLoginHintCommonAsync(authorityInstance, scopes, extraScopesToConsent,
                        loginHint, behavior, extraQueryParameters, parent, ApiEvent.ApiIds.AcquireTokenWithScopeHintBehaviorAuthority).ConfigureAwait(false);
        }

        /// <summary>
        /// Interactive request to acquire token for a given account, with the possibility of controlling the user experience, passing extra query
        /// parameters, providing extra scopes that the user can pre-consent to, and overriding the authority pre-configured in the application
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="account">Account to use for the interactive token acquisition. See <see cref="IAccount"/> for ways to get an account</param>
        /// <param name="behavior">Designed interactive experience for the user.</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="extraScopesToConsent">scopes that you can request the end user to consent upfront, in addition to the scopes for the protected Web API
        /// for which you want to acquire a security token.</param>
        /// <param name="authority">Specific authority for which the token is requested. Passing a different value than configured does not change the configured value</param>
        /// <param name="parent">Object containing a reference to the parent window/activity. REQUIRED for Xamarin.Android only.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(IEnumerable<string> scopes, IAccount account,
        UIBehavior behavior, string extraQueryParameters, IEnumerable<string> extraScopesToConsent, string authority, UIParent parent)
        {
            GuardNetCore();

            Authority authorityInstance =
                Instance.Authority.CreateAuthorityWithOverride(
                    ServiceBundle,
                    AuthorityInfo.FromAuthorityUri(
                       authority,
                       ServiceBundle.Config.DefaultAuthorityInfo.ValidateAuthority,
                       false));
            return
                await
                    AcquireTokenForUserCommonAsync(authorityInstance, scopes, extraScopesToConsent, account,
                        behavior, extraQueryParameters, parent, ApiEvent.ApiIds.AcquireTokenWithScopeUserBehaviorAuthority).ConfigureAwait(false);
        }



        internal IWebUI CreateWebAuthenticationDialog(UIParent parent, UIBehavior behavior, RequestContext requestContext)
        {
            //create instance of UIParent and assign useCorporateNetwork to UIParent
            if (parent == null)
            {
#pragma warning disable CS0618 // Throws a good exception on Android, but ctor cannot be removed for backwards compat reasons
                parent = new UIParent();
#pragma warning restore CS0618 // Type or member is obsolete
            }

#if WINDOWS_APP || DESKTOP
            //hidden webview can be used in both WinRT and desktop applications.
            parent.UseHiddenBrowser = behavior.Equals(UIBehavior.Never);
#if WINDOWS_APP
            parent.UseCorporateNetwork = UseCorporateNetwork;
#endif
#endif

            return ServiceBundle.PlatformProxy.GetWebUiFactory().CreateAuthenticationDialog(parent.CoreUIParent, requestContext);
        }

        private void GuardNetCore()
        {
#if NET_CORE
            throw new PlatformNotSupportedException("On .NET Core, interactive authentication is not supported. " + 
                "Consider using Device Code Flow https://aka.ms/msal-net-device-code-flow or Integrated Windows Auth https://aka.ms/msal-net-iwa");
#endif
        }

        private void GuardUIParentAndroid()
        {
#if ANDROID
            throw new PlatformNotSupportedException("To enable interactive authentication on Android, please call an overload of AcquireTokenAsync that " +
                "takes in an UIParent object, which you should initialize to an Activity. " +
                "See https://aka.ms/msal-interactive-android for details.");
#endif
        }

        private async Task<AuthenticationResult> AcquireTokenForLoginHintCommonAsync(
            Authority authority,
            IEnumerable<string> scopes,
            IEnumerable<string> extraScopesToConsent,
            string loginHint,
            UIBehavior behavior,
            string extraQueryParameters,
            UIParent parent,
            ApiEvent.ApiIds apiId)
        {
            var requestParams = CreateRequestParameters(authority, scopes, null, UserTokenCache);
            requestParams.ExtraQueryParameters = extraQueryParameters;

            var handler = new InteractiveRequest(
                ServiceBundle,
                requestParams,
                apiId,
                extraScopesToConsent,
                loginHint,
                behavior,
                CreateWebAuthenticationDialog(
                    parent,
                    behavior,
                    requestParams.RequestContext));

            return await handler.RunAsync(CancellationToken.None).ConfigureAwait(false);
        }

        private async Task<AuthenticationResult> AcquireTokenForUserCommonAsync(Authority authority, IEnumerable<string> scopes,
            IEnumerable<string> extraScopesToConsent, IAccount user, UIBehavior behavior, string extraQueryParameters, UIParent parent, ApiEvent.ApiIds apiId)
        {
            var requestParams = CreateRequestParameters(authority, scopes, user, UserTokenCache);
            requestParams.ExtraQueryParameters = extraQueryParameters;

            var handler = new InteractiveRequest(
                ServiceBundle,
                requestParams,
                apiId,
                extraScopesToConsent,
                behavior,
                CreateWebAuthenticationDialog(parent, behavior, requestParams.RequestContext));

            return await handler.RunAsync(CancellationToken.None).ConfigureAwait(false);
        }

        internal override AuthenticationRequestParameters CreateRequestParameters(Authority authority,
            IEnumerable<string> scopes, IAccount user, TokenCache cache)
        {
            AuthenticationRequestParameters parameters = base.CreateRequestParameters(authority, scopes, user, cache);
            return parameters;
        }

        // endif for !NET_CORE
#endif

        /// <summary>
        /// Acquires a security token on a device without a Web browser, by letting the user authenticate on 
        /// another device. This is done in two steps:
        /// <list type="bullet">
        /// <item><description>the method first acquires a device code from the authority and returns it to the caller via
        /// the <paramref name="deviceCodeResultCallback"/>. This callback takes care of interacting with the user
        /// to direct them to authenticate (to a specific URL, with a code)</description></item>
        /// <item><description>The method then proceeds to poll for the security
        /// token which is granted upon successful login by the user based on the device code information</description></item>
        /// </list>
        /// See https://aka.ms/msal-device-code-flow.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="deviceCodeResultCallback">Callback containing information to show the user about how to authenticate and enter the device code.</param>
        /// <returns>Authentication result containing a token for the requested scopes and for the user who has authenticated on another device with the code</returns>
        public Task<AuthenticationResult> AcquireTokenWithDeviceCodeAsync(
            IEnumerable<string> scopes,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback)
        {
            return AcquireTokenWithDeviceCodeAsync(scopes, string.Empty, deviceCodeResultCallback);
        }

        /// <summary>
        /// Acquires a security token on a device without a Web browser, by letting the user authenticate on 
        /// another device, with possiblity of passing extra parameters. This is done in two steps:
        /// <list type="bullet">
        /// <item><description>the method first acquires a device code from the authority and returns it to the caller via
        /// the <paramref name="deviceCodeResultCallback"/>. This callback takes care of interacting with the user
        /// to direct them to authenticate (to a specific URL, with a code)</description></item>
        /// <item><description>The method then proceeds to poll for the security
        /// token which is granted upon successful login by the user based on the device code information</description></item>
        /// </list>
        /// See https://aka.ms/msal-device-code-flow.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. 
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="deviceCodeResultCallback">Callback containing information to show the user about how to authenticate and enter the device code.</param>
        /// <returns>Authentication result containing a token for the requested scopes and for the user who has authenticated on another device with the code</returns>
        public async Task<AuthenticationResult> AcquireTokenWithDeviceCodeAsync(
            IEnumerable<string> scopes,
            string extraQueryParameters,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback)
        {
            if (deviceCodeResultCallback == null)
            {
                throw new ArgumentNullException("A deviceCodeResultCallback must be provided for Device Code authentication to work properly");
            }

            return await AcquireTokenWithDeviceCodeAsync(
                scopes,
                extraQueryParameters,
                deviceCodeResultCallback,
                CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Acquires a security token on a device without a Web browser, by letting the user authenticate on 
        /// another device, with possiblity of cancelling the token acquisition before it times out. This is done in two steps:
        /// <list type="bullet">
        /// <item><description>the method first acquires a device code from the authority and returns it to the caller via
        /// the <paramref name="deviceCodeResultCallback"/>. This callback takes care of interacting with the user
        /// to direct them to authenticate (to a specific URL, with a code)</description></item>
        /// <item><description>The method then proceeds to poll for the security
        /// token which is granted upon successful login by the user based on the device code information. This step is cancelable</description></item>
        /// </list>
        /// See https://aka.ms/msal-device-code-flow.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="deviceCodeResultCallback">The callback containing information to show the user about how to authenticate and enter the device code.</param>
        /// <param name="cancellationToken">A CancellationToken which can be triggered to cancel the operation in progress.</param>
        /// <returns>Authentication result containing a token for the requested scopes and for the user who has authenticated on another device with the code</returns>
        public Task<AuthenticationResult> AcquireTokenWithDeviceCodeAsync(
            IEnumerable<string> scopes,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback,
            CancellationToken cancellationToken)
        {
            return AcquireTokenWithDeviceCodeAsync(scopes, string.Empty, deviceCodeResultCallback, cancellationToken);
        }

        /// <summary>
        /// Acquires a security token on a device without a Web browser, by letting the user authenticate on 
        /// another device, with possiblity of passing extra query parameters and cancelling the token acquisition before it times out. This is done in two steps:
        /// <list type="bullet">
        /// <item><description>the method first acquires a device code from the authority and returns it to the caller via
        /// the <paramref name="deviceCodeResultCallback"/>. This callback takes care of interacting with the user
        /// to direct them to authenticate (to a specific URL, with a code)</description></item>
        /// <item><description>The method then proceeds to poll for the security
        /// token which is granted upon successful login by the user based on the device code information. This step is cancelable</description></item>
        /// </list>
        /// See https://aka.ms/msal-device-code-flow.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="extraQueryParameters">This parameter will be appended as is to the query string in the HTTP authentication request to the authority. 
        /// This is expected to be a string of segments of the form <c>key=value</c> separated by an ampersand character.
        /// The parameter can be null.</param>
        /// <param name="deviceCodeResultCallback">The callback containing information to show the user about how to authenticate and enter the device code.</param>
        /// <param name="cancellationToken">A CancellationToken which can be triggered to cancel the operation in progress.</param>
        /// <returns>Authentication result containing a token for the requested scopes and for the user who has authenticated on another device with the code</returns>
        public async Task<AuthenticationResult> AcquireTokenWithDeviceCodeAsync(
            IEnumerable<string> scopes,
            string extraQueryParameters,
            Func<DeviceCodeResult, Task> deviceCodeResultCallback,
            CancellationToken cancellationToken)
        {
            Authority authority = Instance.Authority.CreateAuthority(ServiceBundle, Authority, ValidateAuthority);

            var requestParams = CreateRequestParameters(authority, scopes, null, UserTokenCache);
            requestParams.ExtraQueryParameters = extraQueryParameters;

            var handler = new DeviceCodeRequest(
                ServiceBundle,
                requestParams,
                ApiEvent.ApiIds.None,
                deviceCodeResultCallback);

            return await handler.RunAsync(cancellationToken).ConfigureAwait(false);
        }

        #region INTEGRATED WINDOWS AUTH
#if !ANDROID_BUILDTIME && !iOS_BUILDTIME

#if !NET_CORE_BUILDTIME
        /// <summary>
        /// Non-interactive request to acquire a security token for the signed-in user in Windows, via Integrated Windows Authentication.
        /// See https://aka.ms/msal-net-iwa.
        /// The account used in this overrides is pulled from the operating system as the current user principal name
        /// </summary>
        /// <remarks>
        /// On Windows Universal Platform, the following capabilities need to be provided:
        /// Enterprise Authentication, Private Networks (Client and Server), User Account Information
        /// Supported on .net desktop and UWP
        /// </remarks>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <returns>Authentication result containing a token for the requested scopes and for the currently logged-in user in Windows</returns>
        public async Task<AuthenticationResult> AcquireTokenByIntegratedWindowsAuthAsync(IEnumerable<string> scopes)
        {
            GuardNonWindowsFrameworks();
            GuardIWANetCore();

            return await AcquireTokenByIWAAsync(scopes, new IntegratedWindowsAuthInput()).ConfigureAwait(false);
        }
#endif

        /// <summary>
        /// Non-interactive request to acquire a security token for the signed-in user in Windows, via Integrated Windows Authentication.
        /// See https://aka.ms/msal-net-iwa.
        /// The account used in this overrides is pulled from the operating system as the current user principal name
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="username">Identifier of the user account for which to acquire a token with Integrated Windows authentication. 
        /// Generally in UserPrincipalName (UPN) format, e.g. john.doe@contoso.com</param>
        /// <returns>Authentication result containing a token for the requested scopes and for the currently logged-in user in Windows</returns>
        public async Task<AuthenticationResult> AcquireTokenByIntegratedWindowsAuthAsync(
            IEnumerable<string> scopes,
            string username)
        {
            GuardNonWindowsFrameworks();
            return await AcquireTokenByIWAAsync(scopes, new IntegratedWindowsAuthInput(username)).ConfigureAwait(false);
        }

        private async Task<AuthenticationResult> AcquireTokenByIWAAsync(IEnumerable<string> scopes, IntegratedWindowsAuthInput iwaInput)
        {
            Authority authority = Instance.Authority.CreateAuthority(ServiceBundle, Authority, ValidateAuthority);
            var requestParams = CreateRequestParameters(authority, scopes, null, UserTokenCache);
            var handler = new IntegratedWindowsAuthRequest(
                ServiceBundle,
                requestParams,
                ApiEvent.ApiIds.AcquireTokenWithScopeUser,
                iwaInput);

            return await handler.RunAsync(CancellationToken.None).ConfigureAwait(false);
        }

        private static void GuardNonWindowsFrameworks()
        {
#if ANDROID || iOS
            throw new PlatformNotSupportedException("Integrated Windows Authentication is not supported on this platform. " +
                "For details about this authentication flow, please see https://aka.ms/msal-net-iwa");
#endif
        }

        private static void GuardIWANetCore()
        {
#if NET_CORE
            throw new PlatformNotSupportedException("This overload of AcquireTokenByIntegratedWindowsAuthAsync is not suppored on .net core because " +
                "MSAL cannot determine the username (UPN) of the currently logged in user. Please use the overload where you pass in a username (UPN). " +
                "For more details see https://aka.ms/msal-net-iwa");
#endif
        }
#endif
        #endregion INTEGRATED WINDOWS AUTH

        #region USERNAME PASSWORD

        /// <summary>
        /// Non-interactive request to acquire a security token from the authority, via Username/Password Authentication.
        /// Available only on .net desktop and .net core. See https://aka.ms/msal-net-up for details.
        /// </summary>
        /// <param name="scopes">Scopes requested to access a protected API</param>
        /// <param name="username">Identifier of the user application requests token on behalf.
        /// Generally in UserPrincipalName (UPN) format, e.g. john.doe@contoso.com</param>
        /// <param name="securePassword">User password.</param>
        /// <returns>Authentication result containing a token for the requested scopes and account</returns>
        public async Task<AuthenticationResult> AcquireTokenByUsernamePasswordAsync(
            IEnumerable<string> scopes, string username, System.Security.SecureString securePassword)
        {
#if DESKTOP || NET_CORE
            UsernamePasswordInput usernamePasswordInput = new UsernamePasswordInput(username, securePassword);
            return await AcquireTokenByUsernamePasswordAsync(scopes, usernamePasswordInput).ConfigureAwait(false);
#else
            // TODO: need to consolidate this properly with the public API.
            throw new NotImplementedException();
#endif
        }

        private async Task<AuthenticationResult> AcquireTokenByUsernamePasswordAsync(
            IEnumerable<string> scopes, UsernamePasswordInput usernamePasswordInput)
        {
            Authority authority = Instance.Authority.CreateAuthority(ServiceBundle, Authority, ValidateAuthority);
            var requestParams = CreateRequestParameters(authority, scopes, null, UserTokenCache);
            var handler = new UsernamePasswordRequest(
                ServiceBundle,
                requestParams,
                ApiEvent.ApiIds.AcquireTokenWithScopeUser,
                usernamePasswordInput);

            return await handler.RunAsync(CancellationToken.None).ConfigureAwait(false);
        }

        #endregion // USERNAME PASSWORD

        #region PCA WITH TOKEN CACHE
        //TODO: minor bug - we accidentally exposed this ctor to UWP without exposing
        // the TokenCacheExtensions. Not worth removing and breaking backwards compat for it now, 
        // as we plan to expose the whole thing
#if !ANDROID_BUILDTIME && !iOS_BUILDTIME

        /// <summary>
        /// Constructor to create application instance. This constructor is only available for Desktop and NetCore apps
        /// </summary>
        /// <param name="clientId">Client id of the application</param>
        /// <param name="authority">Default authority to be used for the application</param>
        /// <param name="userTokenCache">Instance of TokenCache.</param>
        public PublicClientApplication(string clientId, string authority, TokenCache userTokenCache)
            : this(PublicClientApplicationBuilder.Create(clientId, authority).WithUserTokenCache(userTokenCache).BuildConfiguration())
        {
            GuardOnMobilePlatforms();
        }

        private static void GuardOnMobilePlatforms()
        {
#if ANDROID || iOS
        throw new PlatformNotSupportedException("You should not use this constructor that takes in a TokenCache object on mobile platforms. " +
            "This constructor is meant to allow applications to define their own storage strategy on .net desktop and .net core. " +
            "On mobile platforms, a secure and performant storage mechanism is implemeted by MSAL. " +
            "For more details about custom token cache serialization, visit https://aka.ms/msal-net-serialization");
#endif
        }
#endif
        #endregion // PCA WITH TOKEN CACHE
    }
}