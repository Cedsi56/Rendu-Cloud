# Définition des variables d'entrée
variable "rg_name" {
  type        = string
  description = "Nom du resource group\n-- Exemple : test2"
}

variable "rg_location" {
  type        = string
  description = "Location du resource group\n-- Exemple : westeurope"
}

variable "vn_name" {
  type        = string
  description = "Nom du virtual network\n-- Exemple : testVnetwork"
}

variable "sn_name" {
  type        = string
  description = "Nom du subnetwork\n-- Exemple : testSubNet"
}

variable "nsg_name" {
  type        = string
  description = "Nom du network security group de la vm\n-- Exemple : testNetworkSecurityGroup"
}

variable "ip_name" {
  type        = string
  description = "Nom de l'ip publique de la vm\n-- Exemple : testIp"
}

variable "nic_name" {
  type        = string
  description = "Nom de la network interface\n-- Exemple : testNetworkInterface"
}

variable "ipconfig_name" {
  type        = string
  description = "Nom de l'ip config de la network interface de la vm\n-- Exemple : Primary"
}

variable "vm_name" {
  type        = string
  description = "Nom de la vm\n-- Exemple : test2"
}

variable "vm_login" {
  type        = string
  description = "Nom du compte admin\n-- Exemple : adminuser"
}

variable "vm_password" {
  type        = string
  description = "Nom du mot de passe du compte admin\n-- Exemple : Password123!"
}

variable "disk_name" {
  type        = string
  description = "Nom du disk de la vm\n-- Exemple : testOsDisk"
}

variable "vmext_name" {
  type        = string
  description = "Nom de l'extension de la vm\n-- Exemple : vmExtName"
}

variable "asp_name" {
  type        = string
  description = "The name of the app service plan."
}

variable "asp_sku_name" {
  type        = string
  description = "Sku name of the app service plan."
}

variable "app_name" {
  type        = string
  description = "Name of the app."
}

variable "dotnet_version" {
  type        = string
  description = "The version of .NET to be used"
}
variable "db_server_name" {
  type        = string
  description = "The name of the database server."
}

variable "db_login" {
  type        = string
  description = "The login of the database server."
}

variable "db_password" {
  type        = string
  description = "The password of the database server."
}

variable "db_account_name" {
  type        = string
  description = "The name of the database account."
}

variable "db_name" {
  type        = string
  description = "The name of the database."
}