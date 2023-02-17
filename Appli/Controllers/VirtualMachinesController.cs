using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using jadorelecloudgaming.Data;
using jadorelecloudgaming.Models;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager.Compute.Models;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager;
using Azure;
using System.Diagnostics;
using System.Security.Claims;
using Newtonsoft.Json;
using NuGet.Protocol;
using Mono.TextTemplating;

namespace jadorelecloudgaming.Controllers
{
    public class VirtualMachinesController : Controller
    {
        /// <summary>
        /// The db context
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// The application's tenant id
        /// </summary>
        private const string TENANT_ID = "b7b023b8-7c32-4c02-92a6-c8cdaa1d189c";

        /// <summary>
        /// The application's client id
        /// </summary>
        private const string CLIENT_ID = "35f78706-f678-4b7c-8059-acfb1cb38f35";

        /// <summary>
        /// The application's client secret
        /// </summary>
        private const string CLIENT_SECRET = "hTl8Q~db1oqP5bxqXh5L13J0~ZPqV.Se71EOcbWR";

        /// <summary>
        /// The location used for resource groups and vms
        /// </summary>
        private readonly AzureLocation LOCATION = AzureLocation.WestEurope;

        /// <summary>
        /// Create and initialize the controller
        /// </summary>
        /// <param name="context">The db context</param>
        public VirtualMachinesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: VirtualMachines
        /// <summary>
        /// Displays a view with the user's virtual machine in a list format
        /// If the user is not connected, the list is empty
        /// </summary>
        public async Task<IActionResult> Index()
        {
            return _context.VirtualMachines != null
                ? View(await _context.VirtualMachines.Where(vm => vm.name == GetUserName()).ToListAsync())
                : Problem("Entity set 'ApplicationDbContext.VirtualMs'  is null.");
        }

        /// <summary>
        /// Returns the connected user's name
        /// </summary>
        /// <returns>The connected user's name</returns>
        /// <exception cref="NullReferenceException">Raised if the user is not connected</exception>
        public string GetUserName()
        {
            string? currentUserId = User.FindFirstValue(ClaimTypes.Name);
            if (currentUserId != null)
            {
                currentUserId = currentUserId.Split("@")[0];
                currentUserId = currentUserId.Replace(".", "");
            }
            else
            {
                return "";
            }

            return currentUserId;
        }

        // GET: VirtualMachines/Details/5
        /// <summary>
        /// Displays a virtual machine's details
        /// </summary>
        /// <param name="id">The virtual machine's id</param>
        /// <returns></returns>
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.VirtualMachines == null)
            {
                return NotFound();
            }

            var virtualMachine = await _context.VirtualMachines
                .FirstOrDefaultAsync(m => m.id == id);
            if (virtualMachine == null)
            {
                return NotFound();
            }

            return View(virtualMachine);
        }

        // GET: VirtualMachines/Create
        /// <summary>
        /// Validate the user's input to create a VirtualMachine
        /// The VM will also be created on azure through the MakeVM(virtualMachine) function
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            return View();
        }

        // POST: VirtualMachines/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("id,login,password")] VirtualMachine virtualMachine)
        {
            ModelState.Remove("ip");
            ModelState.Remove("name");
            if (ModelState.IsValid)
            {
                await MakeVM(virtualMachine);
                virtualMachine.Powered = true;
                _context.Add(virtualMachine);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(virtualMachine);
        }

        // GET: VirtualMachines/Edit/5
        /// <summary>
        /// Display a view that allows the user to modify a virtual machine's information
        /// </summary>
        /// <param name="id">The virtual machine's id</param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.VirtualMachines == null)
            {
                return NotFound();
            }

            var virtualMachine = await _context.VirtualMachines.FindAsync(id);
            if (virtualMachine == null)
            {
                return NotFound();
            }
            return View(virtualMachine);
        }

        // POST: VirtualMachines/Edit/5
        /// <summary>
        /// Handle the user's virtual machine modification
        /// </summary>
        /// <param name="id">The virtual machine's id</param>
        /// <param name="virtualMachine">The virtual machine</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id,login,password")] VirtualMachine virtualMachine)
        {
            if (id != virtualMachine.id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(virtualMachine);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VirtualMachineExists(virtualMachine.id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(virtualMachine);
        }

        // GET: VirtualMachines/Delete/5
        /// <summary>
        /// Displays the view that allows the user to delete a virtual machine
        /// </summary>
        /// <param name="id">The virtual machine's id</param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.VirtualMachines == null)
            {
                return NotFound();
            }

            var virtualMachine = await _context.VirtualMachines
                .FirstOrDefaultAsync(m => m.id == id);
            if (virtualMachine == null)
            {
                return NotFound();
            }

            return View(virtualMachine);
        }

        // POST: VirtualMachines/Delete/5
        /// <summary>
        /// Delete a virtual machine
        /// </summary>
        /// <param name="id">The virtual machine's id</param>
        /// <returns></returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.VirtualMachines == null)
            {
                return Problem("Entity set 'ApplicationDbContext.VirtualMachine'  is null.");
            }
            var virtualMachine = await _context.VirtualMachines.FindAsync(id);
            if (virtualMachine != null)
            {
                _context.VirtualMachines.Remove(virtualMachine);

                string username = GetUserName();

                ArmClient client = GetClient();
                SubscriptionResource sub = await client.GetDefaultSubscriptionAsync();
                ResourceGroupCollection resourceGroups = sub.GetResourceGroups();
                ResourceGroupResource resourceGroup = await resourceGroups.GetAsync(virtualMachine.name);

                VirtualMachineResource vm = await resourceGroup.GetVirtualMachines().GetAsync(virtualMachine.name);
                NetworkInterfaceResource nic =
                    await resourceGroup.GetNetworkInterfaces().GetAsync(username + "-NetworkInterface");
                VirtualNetworkResource vn = await resourceGroup.GetVirtualNetworks().GetAsync(username + "-Vnetwork");
                PublicIPAddressResource publicIp =
                    await resourceGroup.GetPublicIPAddresses().GetAsync(username + "-Ip");

                await vm.DeleteAsync(WaitUntil.Completed, forceDeletion: true);
                await nic.DeleteAsync(WaitUntil.Completed);
                await vn.DeleteAsync(WaitUntil.Completed);
                await publicIp.DeleteAsync(WaitUntil.Completed);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Checks whether the vm with the specified id exists
        /// </summary>
        /// <param name="id">The vm's id</param>
        /// <returns>True if the vm exists, false otherwise</returns>
        private bool VirtualMachineExists(int id)
        {
          return (_context.VirtualMachines?.Any(e => e.id == id)).GetValueOrDefault();
        }

        /// <summary>
        /// Create a virtual machine on Azure
        /// </summary>
        /// <param name="vm">The virtual machine object used as data input</param>
        /// <returns></returns>
        private async Task MakeVM(VirtualMachine vm)
        {
            string username = GetUserName();
            vm.name = username;
            string adminLogin = vm.login;
            string adminPassword = vm.password;

            ArmClient client = GetClient();
            SubscriptionResource sub = await client.GetDefaultSubscriptionAsync();
            ResourceGroupCollection resourceGroups = sub.GetResourceGroups();
            ArmOperation<ResourceGroupResource> operation = await resourceGroups.CreateOrUpdateAsync(
                WaitUntil.Completed,
                username,
                new ResourceGroupData(LOCATION)
            );
            ResourceGroupResource resourceGroup = operation.Value;

            VirtualMachineCollection vms = resourceGroup.GetVirtualMachines();
            NetworkInterfaceCollection nics = resourceGroup.GetNetworkInterfaces();
            VirtualNetworkCollection vns = resourceGroup.GetVirtualNetworks();
            PublicIPAddressCollection publicIps = resourceGroup.GetPublicIPAddresses();

            PublicIPAddressResource ipResource = publicIps.CreateOrUpdate(
            WaitUntil.Completed,
            username + "-Ip",
            new PublicIPAddressData()
            {
                PublicIPAddressVersion = NetworkIPVersion.IPv4,
                PublicIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                Location = LOCATION
            }).Value;

            VirtualNetworkResource vnetResrouce = vns.CreateOrUpdate(
                WaitUntil.Completed,
                username + "-Vnetwork",
                new VirtualNetworkData()
                {
                    Location = LOCATION,
                    Subnets =
                    {
                        new SubnetData()
                        {
                            Name = username + "SubNet",
                            AddressPrefix = "10.0.0.0/24"
                        }
                    },
                    AddressPrefixes =
                    {
                        "10.0.0.0/16"
                    },
                }).Value;

            NetworkInterfaceResource nicResource = nics.CreateOrUpdate(
                WaitUntil.Completed,
                username + "-NetworkInterface",
                new NetworkInterfaceData()
                {
                    Location = LOCATION,
                    IPConfigurations =
                    {
                        new NetworkInterfaceIPConfigurationData()
                        {
                            Name = "Primary",
                            Primary = true,
                            Subnet = new SubnetData() { Id = vnetResrouce?.Data.Subnets.First().Id },
                            PrivateIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                            PublicIPAddress = new PublicIPAddressData() { Id = ipResource?.Data.Id }
                        }
                    }
                }).Value;

            VirtualMachineResource vmResource = vms.CreateOrUpdate(
                WaitUntil.Completed,
                username,
                new VirtualMachineData(LOCATION)
                {
                    HardwareProfile = new VirtualMachineHardwareProfile()
                    {
                        VmSize = VirtualMachineSizeType.StandardB2S
                    },
                    OSProfile = new VirtualMachineOSProfile()
                    {
                        ComputerName = username,
                        AdminUsername = adminLogin,
                        AdminPassword = adminPassword,
                        LinuxConfiguration = new LinuxConfiguration()
                        {
                            DisablePasswordAuthentication = false,
                            ProvisionVmAgent = true
                        }
                    },
                    StorageProfile = new VirtualMachineStorageProfile()
                    {
                        OSDisk = new VirtualMachineOSDisk(DiskCreateOptionType.FromImage)
                        {
                            DeleteOption = DiskDeleteOptionType.Delete,
                        },
                        ImageReference = new ImageReference()
                        {
                            Offer = "UbuntuServer",
                            Publisher = "Canonical",
                            Sku = "18.04-LTS",
                            Version = "latest"
                        }
                    },
                    NetworkProfile = new VirtualMachineNetworkProfile()
                    {
                        NetworkInterfaces =
                        {
                            new VirtualMachineNetworkInterfaceReference()
                            {
                                Id = nicResource.Id
                            }
                        }
                    },
                }
            ).Value;

            Debug.Write(resourceGroup.Id);

            vm.ip = InitIp(resourceGroup).Data.IPAddress;
        }

        /// <summary>
        /// Turn off a virtual machine (Stop button on the UI)
        /// </summary>
        /// <param name="id">The virtual machine's id</param>
        /// <returns></returns>
        public async Task<IActionResult> TurnOffVM(int id)
        {
            if (_context.VirtualMachines == null)
            {
                return NotFound();
            }

            var virtualMachine = await _context.VirtualMachines
                .FirstOrDefaultAsync(m => m.id == id);
            if (virtualMachine == null)
            {
                return NotFound();
            }

            virtualMachine.Powered = false;


            ArmClient client = GetClient();
            SubscriptionResource sub = await client.GetDefaultSubscriptionAsync();
            ResourceGroupCollection resourceGroups = sub.GetResourceGroups();
            ArmOperation<ResourceGroupResource> operation = await resourceGroups.CreateOrUpdateAsync(
                    WaitUntil.Completed,
                    virtualMachine.name,
                    new ResourceGroupData(LOCATION)
                );
            ResourceGroupResource resourceGroup = operation.Value;

            var vms = resourceGroup.GetVirtualMachines();

            foreach (var myVm in vms)
            {
                await myVm.PowerOffAsync(WaitUntil.Completed);
            }

            _context.Update(virtualMachine);
            await _context.SaveChangesAsync();

            return View(virtualMachine);
        }

        /// <summary>
        /// Turn on a virtual machine (Start button on the UI)
        /// </summary>
        /// <param name="id">The virtual machine's id</param>
        /// <returns></returns>
        public async Task<IActionResult> TurnOnVM(int id)
        {
            if (_context.VirtualMachines == null)
            {
                return NotFound();
            }

            var virtualMachine = await _context.VirtualMachines
                .FirstOrDefaultAsync(m => m.id == id);
            if (virtualMachine == null)
            {
                return NotFound();
            }

            virtualMachine.Powered = true;


            ArmClient client = GetClient();
            SubscriptionResource sub = await client.GetDefaultSubscriptionAsync();
            ResourceGroupCollection resourceGroups = sub.GetResourceGroups();
            ArmOperation<ResourceGroupResource> operation = await resourceGroups.CreateOrUpdateAsync(
                    WaitUntil.Completed,
                    virtualMachine.name,
                    new ResourceGroupData(LOCATION)
                );
            ResourceGroupResource resourceGroup = operation.Value;

            var vms = resourceGroup.GetVirtualMachines();

            foreach (var myVm in vms)
            {
                await myVm.PowerOnAsync(WaitUntil.Completed);
            }

            _context.Update(virtualMachine);
            await _context.SaveChangesAsync();

            return View(virtualMachine);
        }

        /// <summary>
        /// After the vm is created, this method is called to fetch its ip
        /// </summary>
        /// <param name="resourceGroup">The vm's resource group</param>
        /// <returns></returns>
        private PublicIPAddressResource InitIp(ResourceGroupResource resourceGroup)
        {
            var publicIps = resourceGroup.GetPublicIPAddresses();
            var ipResource = publicIps.CreateOrUpdate(
                WaitUntil.Completed,
                GetUserName() + "-Ip",
                new PublicIPAddressData()
                {
                    PublicIPAddressVersion = NetworkIPVersion.IPv4,
                    PublicIPAllocationMethod = NetworkIPAllocationMethod.Dynamic,
                    Location = LOCATION
                }).Value;

            return ipResource;
        }

        /// <summary>
        /// Return the Azure Client used to create and delete vms
        /// </summary>
        /// <returns>The Azure Client</returns>
        private ArmClient GetClient()
        {
            return new(new ClientSecretCredential(TENANT_ID, CLIENT_ID, CLIENT_SECRET));
        }
    }
}
