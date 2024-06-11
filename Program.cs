// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager.Samples.Common;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;

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
            string vnetName = Utilities.CreateRandomName("vnet");
            string pipName = Utilities.CreateRandomName("pip");
            string vpnGatewayName = Utilities.CreateRandomName("vngw");
            string localGatewayName = Utilities.CreateRandomName("lngw");
            string connectionName = Utilities.CreateRandomName("con");

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
                VirtualNetworkData vnetInput = new VirtualNetworkData()
                {
                    Location = resourceGroup.Data.Location,
                    AddressPrefixes = { "10.11.0.0/16" },
                    Subnets =
                    {
                        new SubnetData() { AddressPrefix = "10.11.255.0/27", Name = "GatewaySubnet" },
                        new SubnetData() { AddressPrefix = "10.11.0.0/24", Name = "Subnet1" }
                    },
                };
                var vnetLro = await resourceGroup.GetVirtualNetworks().CreateOrUpdateAsync(WaitUntil.Completed, vnetName, vnetInput);
                VirtualNetworkResource vnet = vnetLro.Value;
                Utilities.Log($"Created a virtual network: {vnet.Data.Name}");

                //============================================================
                // Create public ip for virtual network gateway
                var pip = await Utilities.CreatePublicIP(resourceGroup, pipName);

                // Create VPN gateway
                Utilities.Log("Creating virtual network gateway...");
                VirtualNetworkGatewayData vpnGatewayInput = new VirtualNetworkGatewayData()
                {
                    Location = resourceGroup.Data.Location,
                    Sku = new VirtualNetworkGatewaySku()
                    {
                        Name = VirtualNetworkGatewaySkuName.Basic,
                        Tier = VirtualNetworkGatewaySkuTier.Basic
                    },
                    Tags = { { "key", "value" } },
                    EnableBgp = false,
                    GatewayType = VirtualNetworkGatewayType.Vpn,
                    VpnType = VpnType.RouteBased,
                    IPConfigurations =
                    {
                        new VirtualNetworkGatewayIPConfiguration()
                        {
                            Name = Utilities.CreateRandomName("config"),
                            PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                            PublicIPAddressId  = pip.Data.Id,
                            SubnetId = vnet.Data.Subnets.First(item => item.Name == "GatewaySubnet").Id,
                        }
                    }
                };
                var vpnGatewayLro = await resourceGroup.GetVirtualNetworkGateways().CreateOrUpdateAsync(WaitUntil.Completed, vpnGatewayName, vpnGatewayInput);
                VirtualNetworkGatewayResource vpnGateway = vpnGatewayLro.Value;
                Utilities.Log($"Created virtual network gateway: {vpnGateway.Data.Name}");

                //============================================================
                // Create local network gateway
                Utilities.Log("Creating virtual network gateway...");
                LocalNetworkGatewayData localNetworkGatewayInput = new LocalNetworkGatewayData()
                {
                    Location = resourceGroup.Data.Location,
                    GatewayIPAddress = "40.71.184.214",
                    LocalNetworkAddressPrefixes = { "192.168.3.0/24" }
                };
                var localNetworkGatewayLro = await resourceGroup.GetLocalNetworkGateways().CreateOrUpdateAsync(WaitUntil.Completed, localGatewayName, localNetworkGatewayInput);
                LocalNetworkGatewayResource localNetworkGateway = localNetworkGatewayLro.Value;
                Utilities.Log($"Created virtual network gateway: {localNetworkGateway.Data.Name}");

                //============================================================
                // Create VPN Site-to-Site connection
                Utilities.Log("Creating virtual network gateway connection...");
                VirtualNetworkGatewayConnectionType connectionType = VirtualNetworkGatewayConnectionType.IPsec;
                VirtualNetworkGatewayConnectionData gatewayConnectionInput = new VirtualNetworkGatewayConnectionData(vpnGateway.Data, connectionType)
                {
                    Location = resourceGroup.Data.Location,
                    LocalNetworkGateway2 = localNetworkGateway.Data,
                    SharedKey = "MySecretKey"
                };
                var connectionLro = await resourceGroup.GetVirtualNetworkGatewayConnections().CreateOrUpdateAsync(WaitUntil.Completed, vpnGatewayName, gatewayConnectionInput);
                VirtualNetworkGatewayConnectionResource connection = connectionLro.Value;
                Utilities.Log($"Created virtual network gateway connection: {connection.Data.Name}");

                //============================================================
                // List VPN Gateway connections for particular gateway
                Utilities.Log("List VPN Gateway connections for particular gateway:");
                await foreach (var conn in resourceGroup.GetVirtualNetworkGatewayConnections().GetAllAsync())
                {
                    Utilities.Log(conn.Data.Name);
                }
                //============================================================
                // Reset virtual network gateway
                vpnGateway.Reset(WaitUntil.Started);
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