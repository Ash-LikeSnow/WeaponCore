using System;
using System.Collections.Generic;
using CoreSystems.Support;
using Sandbox.ModAPI;

namespace CoreSystems.Api
{
    public class ApiServer
    {
        private const long Channel = 67549756549;

        /// <summary>
        /// Is the API ready to be serve
        /// </summary>
        public bool IsReady { get; private set; }

        private void HandleMessage(object o)
        {
            if ((o as string) == "ApiEndpointRequest")
                MyAPIGateway.Utilities.SendModMessage(Channel, Session.I.Api.ModApiMethods);
        }

        private bool _isRegistered;

        /// <summary>
        /// Prepares the client to receive API endpoints and requests an update.
        /// </summary>
        public void Load()
        {
            if (!_isRegistered)
            {
                _isRegistered = true;
                MyAPIGateway.Utilities.RegisterMessageHandler(Channel, HandleMessage);
            }
            IsReady = true;
            try
            {
                MyAPIGateway.Utilities.SendModMessage(Channel, Session.I.Api.ModApiMethods);

            }
            catch (Exception ex) { Log.Line($"Exception in Api Load: {ex}", null, true); }
        }


        /// <summary>
        /// Unloads all API endpoints and detaches events.
        /// </summary>
        public void Unload()
        {
            if (_isRegistered)
            {
                _isRegistered = false;
                MyAPIGateway.Utilities.UnregisterMessageHandler(Channel, HandleMessage);
            }
            IsReady = false;
            MyAPIGateway.Utilities.SendModMessage(Channel, new Dictionary<string, Delegate>());
        }
    }
}
