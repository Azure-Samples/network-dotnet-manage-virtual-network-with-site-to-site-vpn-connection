---
services: virtual-network
platforms: dotnet
author: yaohaizh
---

# Manage virtual network with site-to-site VPN connection #

          Azure Network sample for managing virtual network gateway.
           - Create virtual network with gateway subnet
           - Create VPN gateway
           - Create local network gateway
           - Create VPN Site-to-Site connection
           - List VPN Gateway connections for particular gateway
           - Reset virtual network gateway


## Running this Sample ##

To run this sample:

Set the environment variable `AZURE_AUTH_LOCATION` with the full path for an auth file. See [how to create an auth file](https://github.com/Azure/azure-libraries-for-net/blob/master/AUTH.md).

    git clone https://github.com/Azure-Samples/network-dotnet-manage-virtual-network-with-site-to-site-vpn-connection.git

    cd network-dotnet-manage-virtual-network-with-site-to-site-vpn-connection

    dotnet restore

    dotnet run

## More information ##

[Azure Management Libraries for C#](https://github.com/Azure/azure-sdk-for-net/tree/Fluent)
[Azure .Net Developer Center](https://azure.microsoft.com/en-us/develop/net/)
If you don't have a Microsoft Azure subscription you can get a FREE trial account [here](http://go.microsoft.com/fwlink/?LinkId=330212)

---

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.