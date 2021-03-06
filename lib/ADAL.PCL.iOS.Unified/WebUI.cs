//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class WebUI : IWebUI
    {
        private static SemaphoreSlim returnedUriReady;
        private static AuthorizationResult authorizationResult;
        private readonly AuthorizationParameters parameters;

        public WebUI(IAuthorizationParameters parameters)
        {
            this.parameters = parameters as AuthorizationParameters;
            if (this.parameters == null)
            {
                throw new ArgumentException("parameters should be of type AuthorizationParameters", "parameters");
            }
        }

        public async Task<AuthorizationResult> AcquireAuthorizationAsync(Uri authorizationUri, Uri redirectUri, CallState callState)
        {
            returnedUriReady = new SemaphoreSlim(0);
            Authenticate(authorizationUri, redirectUri, callState);
            await returnedUriReady.WaitAsync();
            return authorizationResult;
        }

        public static void SetAuthorizationResult(AuthorizationResult authorizationResultInput)
        {
            authorizationResult = authorizationResultInput;
            returnedUriReady.Release();
        }

        public void Authenticate(Uri authorizationUri, Uri redirectUri, CallState callState)
        {
            try
            {
                this.parameters.CallerViewController.PresentViewController(new AuthenticationAgentUINavigationController(authorizationUri.AbsoluteUri, redirectUri.AbsoluteUri, CallbackMethod), false, null);
            }
            catch (Exception ex)
            {
                var adalEx = new AdalException(AdalError.AuthenticationUiFailed, ex);
                PlatformPlugin.Logger.LogException(callState, ex);
                throw adalEx;
            }
        }

        private void CallbackMethod(AuthorizationResult result)
        {
            SetAuthorizationResult(result);
        }
    }
}
