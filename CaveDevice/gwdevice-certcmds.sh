 git clone https://github.com/Azure/iotedge.git
Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope CurrentUser
. .\ca-certs.ps1

ﬀ  New-CACertsCertChain rsa
New-CACertsEdgeDeviceIdentity "gw-leaf-device"
New-CACertsEdgeDevice "EdgeCA"

pscp