// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager.Resources.Models;
using Azure.ResourceManager.Samples.Common;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Storage.Models;
using Azure.ResourceManager.Storage;
using Microsoft.Identity.Client.Extensions.Msal;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Compute;

namespace ManageVpnGatewaySite2SiteConnection
{
    public class Program
    {
        private static ResourceIdentifier? _resourceGroupId = null;

        /**
         * Azure Network sample for managing virtual network gateway.
         *  - Create virtual network with gateway subnet
         *  - Create VPN gateway
         *  - Create local network gateway
         *  - Create VPN Site-to-Site connection
         *  - List VPN Gateway connections for particular gateway
         *  - Reset virtual network gateway
         */
        public static async Task RunSample(ArmClient client)
        {
            string rgName = Utilities.CreateRandomName("NetworkSampleRG");
            string vnetName = SdkContext.RandomResourceName("vnet", 20);
            string vpnGatewayName = SdkContext.RandomResourceName("vngw", 20);
            string localGatewayName = SdkContext.RandomResourceName("lngw", 20);
            string connectionName = SdkContext.RandomResourceName("con", 20);

            try
            {
                // Get default subscription
                SubscriptionResource subscription = await client.GetDefaultSubscriptionAsync();

                // Create a resource group in the EastUS region
                Utilities.Log($"Creating resource group...");
                ArmOperation<ResourceGroupResource> rgLro = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, rgName, new ResourceGroupData(AzureLocation.EastUS));
                ResourceGroupResource resourceGroup = rgLro.Value;
                _resourceGroupId = resourceGroup.Id;
                Utilities.Log("Created a resource group with name: " + resourceGroup.Data.Name);

                //============================================================
                // Create virtual network
                Utilities.Log("Creating virtual network...");
                INetwork network = azure.Networks.Define(vnetName)
                    .WithRegion(region)
                    .WithNewResourceGroup(rgName)
                    .WithAddressSpace("10.11.0.0/16")
                    .WithSubnet("GatewaySubnet", "10.11.255.0/27")
                    .Create();
                Utilities.Log("Created network");
                // Print the virtual network
                Utilities.PrintVirtualNetwork(network);

                //============================================================
                // Create VPN gateway
                Utilities.Log("Creating virtual network gateway...");
                IVirtualNetworkGateway vngw = azure.VirtualNetworkGateways.Define(vpnGatewayName)
                    .WithRegion(region)
                    .WithExistingResourceGroup(rgName)
                    .WithExistingNetwork(network)
                    .WithRouteBasedVpn()
                    .WithSku(VirtualNetworkGatewaySkuName.VpnGw1)
                    .Create();
                Utilities.Log("Created virtual network gateway");

                //============================================================
                // Create local network gateway
                Utilities.Log("Creating virtual network gateway...");
                ILocalNetworkGateway lngw = azure.LocalNetworkGateways.Define(localGatewayName)
                    .WithRegion(region)
                    .WithExistingResourceGroup(rgName)
                    .WithIPAddress("40.71.184.214")
                    .WithAddressSpace("192.168.3.0/24")
                    .Create();
                Utilities.Log("Created virtual network gateway");

                //============================================================
                // Create VPN Site-to-Site connection
                Utilities.Log("Creating virtual network gateway connection...");
                vngw.Connections
                    .Define(connectionName)
                    .WithSiteToSite()
                    .WithLocalNetworkGateway(lngw)
                    .WithSharedKey("MySecretKey")
                    .Create();
                Utilities.Log("Created virtual network gateway connection");

                //============================================================
                // List VPN Gateway connections for particular gateway
                var connections = vngw.ListConnections();
                foreach (var connection in connections)
                {
                    Utilities.Print(connection);
                }
                //============================================================
                // Reset virtual network gateway
                vngw.Reset();
            }
            finally
            {
                try
                {
                    if (_resourceGroupId is not null)
                    {
                        Utilities.Log($"Deleting Resource Group...");
                        await client.GetResourceGroupResource(_resourceGroupId).DeleteAsync(WaitUntil.Completed);
                        Utilities.Log($"Deleted Resource Group: {_resourceGroupId.Name}");
                    }
                }
                catch (NullReferenceException)
                {
                    Utilities.Log("Did not create any resources in Azure. No clean up is necessary");
                }
                catch (Exception ex)
                {
                    Utilities.Log(ex);
                }
            }
        }

        public static async Task Main(string[] args)
        {
            try
            {
                //=================================================================
                // Authenticate
                var clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
                var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
                var tenantId = Environment.GetEnvironmentVariable("TENANT_ID");
                var subscription = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID");
                ClientSecretCredential credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                ArmClient client = new ArmClient(credential, subscription);

                await RunSample(client);
            }
            catch (Exception ex)
            {
                Utilities.Log(ex);
            }
        }
    }
}