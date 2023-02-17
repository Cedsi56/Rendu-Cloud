#! main.tf
# Définition des ressources Azure
terraform {
  required_version = "1.3.8"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "3.43.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~>3.0"
    }
    tls = {
      source  = "hashicorp/tls"
      version = "~>4.0"
    }
  }
}

provider "azurerm" {
  features {
    # azurerm features
  }
  subscription_id = "52ab4a40-5faa-4f34-b0f2-6bad00c12511"
  tenant_id       = "b7b023b8-7c32-4c02-92a6-c8cdaa1d189c"
}

resource "azurerm_resource_group" "rg" {
  name     = var.rg_name
  location = var.rg_location
}

resource "azurerm_virtual_network" "vnet" {
  name                = var.vn_name
  address_space       = ["10.0.0.0/16"]
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
}

resource "azurerm_subnet" "subnet" {
  name                 = var.sn_name
  resource_group_name  = azurerm_resource_group.rg.name
  virtual_network_name = azurerm_virtual_network.vnet.name
  address_prefixes     = ["10.0.2.0/24"]
}

resource "azurerm_public_ip" "vmpublicip" {
  name                = var.ip_name
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  allocation_method   = "Dynamic"
  sku                 = "Basic"
}

resource "azurerm_network_security_group" "vmnsg" {
  name                = var.nsg_name
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name

  security_rule {
    name                       = "SSH"
    priority                   = 1001
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "22"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }
}

resource "azurerm_network_interface" "vmnic" {
  name                = var.nic_name
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name

  ip_configuration {
    name                          = var.ipconfig_name
    subnet_id                     = azurerm_subnet.subnet.id
    private_ip_address_allocation = "Dynamic"
    public_ip_address_id          = azurerm_public_ip.vmpublicip.id
  }
}

resource "azurerm_network_interface_security_group_association" "assoc" {
  network_interface_id      = azurerm_network_interface.vmnic.id
  network_security_group_id = azurerm_network_security_group.vmnsg.id
}

# Generate random text for a unique storage account name
resource "random_id" "random_id" {
  keepers = {
    # Generate a new ID only when a new resource group is defined
    resource_group = azurerm_resource_group.rg.name
  }

  byte_length = 8
}

# Create storage account for boot diagnostics
resource "azurerm_storage_account" "my_storage_account" {
  name                     = "diag${random_id.random_id.hex}"
  location                 = azurerm_resource_group.rg.location
  resource_group_name      = azurerm_resource_group.rg.name
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

# Create (and display) an SSH key
resource "tls_private_key" "example_ssh" {
  algorithm = "RSA"
  rsa_bits  = 4096
}

# Create virtual machine
resource "azurerm_linux_virtual_machine" "vm" {
  name                  = var.vm_name
  location              = azurerm_resource_group.rg.location
  resource_group_name   = azurerm_resource_group.rg.name
  network_interface_ids = [azurerm_network_interface.vmnic.id]
  size                  = "Standard_B2s"

  os_disk {
    name                 = var.disk_name
    caching              = "ReadWrite"
    storage_account_type = "Standard_LRS"
  }

  source_image_reference {
    publisher = "Canonical"
    offer     = "UbuntuServer"
    sku       = "18.04-LTS"
    version   = "latest"
  }

  computer_name  = var.vm_name
  admin_username = var.vm_login
  admin_password = var.vm_password

  disable_password_authentication = false

  admin_ssh_key {
    username   = var.vm_login
    public_key = tls_private_key.example_ssh.public_key_openssh
  }

  boot_diagnostics {
    storage_account_uri = azurerm_storage_account.my_storage_account.primary_blob_endpoint
  }
}

resource "azurerm_virtual_machine_extension" "vmextension" {
  name                 = var.vmext_name
  virtual_machine_id   = azurerm_linux_virtual_machine.vm.id
  publisher            = "Microsoft.Azure.Extensions"
  type                 = "CustomScript"
  type_handler_version = "2.0"

  settings = <<SETTINGS
 {
  "commandToExecute": "sudo apt update && sudo apt install nsnake"
 }
SETTINGS


  tags = {
    environment = "Production"
  }
}





# DB

resource "azurerm_service_plan" "asp_gaming" {
  name                = var.asp_name
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  os_type             = "Windows"
  sku_name            = var.asp_sku_name
}

resource "azurerm_windows_web_app" "webapp_gaming" {
  name                = var.app_name
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_service_plan.asp_gaming.location
  service_plan_id     = azurerm_service_plan.asp_gaming.id

  site_config {
    application_stack {
      dotnet_version = var.dotnet_version
    }
  }

  app_settings = {
    ASPNETCORE_ENVIRONMENT = "Development"
  }
}

resource "azurerm_mssql_server" "db_server_1" {
  name                         = var.db_server_name
  resource_group_name          = azurerm_resource_group.rg.name
  location                     = azurerm_service_plan.asp_gaming.location
  version                      = "12.0"
  administrator_login          = var.db_login
  administrator_login_password = var.db_password

  tags = {
    environment = "production"
  }
}

resource "azurerm_storage_account" "db_account_1" {
  name                     = var.db_account_name
  resource_group_name      = azurerm_resource_group.rg.name
  location                 = azurerm_service_plan.asp_gaming.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_mssql_database" "db_vm" {
  name           = var.db_name
  server_id      = azurerm_mssql_server.db_server_1.id
  collation      = "SQL_Latin1_General_CP1_CI_AS"
  license_type   = "LicenseIncluded"
  max_size_gb    = 1
  read_scale     = false
  sku_name       = "S0"
  zone_redundant = false
}

# The Azure feature Allow access to Azure services can be enabled by setting start_ip_address and end_ip_address to 0.0.0.0
resource "azurerm_mssql_firewall_rule" "firewall" {
  name             = "FirewallRule1"
  server_id        = azurerm_mssql_server.db_server_1.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}