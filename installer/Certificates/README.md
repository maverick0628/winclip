# Code Signing Certificates

This directory is used for storing code signing certificates. Certificates should **never** be committed to source control for security reasons.

## Overview

Code signing is essential for Windows applications to:
- Prevent security warnings during installation
- Establish trust and verify publisher identity
- Enable Windows SmartScreen to recognize your application
- Meet Microsoft Store submission requirements

## Production Certificates

### Obtaining a Commercial Certificate

For production releases, you must obtain a code signing certificate from a trusted Certificate Authority (CA):

**Recommended Providers:**
- [DigiCert](https://www.digicert.com/signing/code-signing-certificates) - Industry standard, widely trusted
- [Sectigo](https://sectigo.com/ssl-certificates-tls/code-signing) - Cost-effective option with good support
- [GlobalSign](https://www.globalsign.com/en/code-signing-certificate) - Strong reputation, Microsoft-trusted

**Certificate Types:**
- **Standard Code Signing** - Basic code signing for applications ($200-$400/year)
- **EV Code Signing** - Extended Validation with immediate SmartScreen reputation ($300-$500/year)
  - Recommended for production due to instant SmartScreen trust
  - Requires hardware token (USB) for storage
  - No "unknown publisher" warnings from day one

**Requirements:**
- Business verification (D-Uns number or legal registration)
- Identity verification documents
- Processing time: 3-7 business days
- Valid for 1-3 years

### Certificate Format

Code signing certificates typically come in two formats:
- **PFX/P12** - Password-protected certificate with private key (most common)
- **Hardware Token** - USB device for EV certificates (highest security)

### Storing Production Certificates

**Security Best Practices:**
1. **Never commit certificates to source control**
2. Store certificates in a secure location (password manager, Azure Key Vault, etc.)
3. Use strong passwords for PFX files
4. Restrict access to authorized personnel only
5. For CI/CD pipelines, use secure secret storage (GitHub Secrets, Azure Key Vault)

**File Placement:**
- Place your production certificate in this directory (installer/Certificates/)
- The directory is excluded from git via `.gitignore`
- Use descriptive names: `WinClip-Production-2026.pfx`

## Test Certificates (Development Only)

For development and testing, you can create a self-signed certificate using the provided script.

### Creating a Test Certificate

```powershell
# Run from the installer/Scripts directory
.\create-test-cert.ps1
```

This will create:
- A self-signed certificate for code signing
- Export to `installer/Certificates/WinClip-Test.pfx`
- Install to your local certificate store (optional)

**Parameters:**
- `-CertificateName` - Subject name for the certificate (default: "WinClip Test Certificate")
- `-Password` - Password for the PFX file (default: "test123")
- `-OutputPath` - Where to save the PFX file (default: installer/Certificates/)
- `-ExportToFile` - Export certificate to PFX file (default: true)
- `-InstallToStore` - Install certificate to local machine store (default: false)

**Example:**
```powershell
.\create-test-cert.ps1 -CertificateName "WinClip Development" -Password "MySecurePassword123" -InstallToStore
```

### Installing Test Certificate for Testing

To test signed installers on your development machine, you need to trust the test certificate:

1. **Create the test certificate** (if not already done):
   ```powershell
   .\create-test-cert.ps1 -InstallToStore
   ```

2. **Manually import the certificate** (if needed):
   - Double-click `WinClip-Test.pfx`
   - Select "Local Machine" → Next
   - Enter password → Next
   - Choose "Place all certificates in the following store"
   - Browse → Select "Trusted Root Certification Authorities"
   - Complete the wizard

3. **Verify installation**:
   - Open `certmgr.msc` (Certificate Manager)
   - Navigate to Trusted Root Certification Authorities → Certificates
   - Look for "WinClip Test Certificate"

**⚠️ Security Warning:**
Test certificates should **only** be installed on development machines. Never distribute installers signed with test certificates to end users.

## Signing Workflow

### Development Workflow

1. Create a test certificate (first time only)
2. Build your installer packages
3. Sign packages with test certificate
4. Install on local machine for testing

```powershell
# Create test certificate (one time)
cd installer/Scripts
.\create-test-cert.ps1 -InstallToStore

# Build and sign
.\build-msix.ps1 -Architecture x64
.\sign-package.ps1 -PackagePath ..\Output\WinClip_x64.msix -CertificatePath ..\Certificates\WinClip-Test.pfx -Password "test123"
```

### Production Workflow

1. Obtain commercial code signing certificate from CA
2. Store certificate securely in `installer/Certificates/` (not in git)
3. Build your installer packages
4. Sign packages with production certificate
5. Verify signature before distribution

```powershell
# Build packages
.\build-msix.ps1 -Architecture x64
.\build-installer.ps1 -Architecture x64

# Sign with production certificate
.\sign-package.ps1 -PackagePath ..\Output\WinClip_x64.msix -CertificatePath ..\Certificates\WinClip-Production.pfx -Password $env:CERT_PASSWORD

.\sign-package.ps1 -PackagePath ..\Output\WinClip-Setup-x64-1.0.0.exe -CertificatePath ..\Certificates\WinClip-Production.pfx -Password $env:CERT_PASSWORD
```

### CI/CD Integration

For automated builds, store certificate and password as secrets:

**GitHub Actions Example:**
```yaml
- name: Sign Packages
  env:
    CERT_BASE64: ${{ secrets.CODE_SIGNING_CERT }}
    CERT_PASSWORD: ${{ secrets.CERT_PASSWORD }}
  run: |
    # Decode certificate from base64
    [Convert]::FromBase64String($env:CERT_BASE64) | Set-Content -Path cert.pfx -Encoding Byte

    # Sign packages
    .\installer\Scripts\sign-package.ps1 -CertificatePath cert.pfx -Password $env:CERT_PASSWORD
```

## Microsoft Store Submission

For Microsoft Store submissions:
- MSIX packages must be signed
- You can use your own certificate or let Microsoft sign during submission
- EV certificates are recommended for better SmartScreen reputation

## Troubleshooting

### "Certificate not trusted" error
- Ensure test certificate is installed in "Trusted Root Certification Authorities"
- For production, verify certificate chain is complete

### "SignTool not found" error
- Install Windows SDK (included in Visual Studio)
- SignTool location: `C:\Program Files (x86)\Windows Kits\10\bin\<version>\x64\signtool.exe`

### "Invalid certificate password" error
- Verify password is correct
- Check for special characters that may need escaping in PowerShell

### Signature verification
```powershell
# Verify MSIX signature
signtool verify /pa /v installer\Output\WinClip_x64.msix

# Verify EXE signature
signtool verify /pa /v installer\Output\WinClip-Setup-x64-1.0.0.exe
```

## Additional Resources

- [Microsoft: Code Signing Best Practices](https://docs.microsoft.com/windows/win32/seccrypto/cryptography-tools)
- [DigiCert: Code Signing Guide](https://www.digicert.com/code-signing/)
- [Windows App Certification Kit](https://docs.microsoft.com/windows/uwp/debug-test-perf/windows-app-certification-kit)
- [MSIX Packaging Documentation](https://docs.microsoft.com/windows/msix/)

## Certificate Renewal

**Important:** Code signing certificates expire after 1-3 years. Plan for renewal:
1. Set calendar reminder 30 days before expiration
2. Request renewal from your CA provider
3. Update build scripts with new certificate path
4. Re-sign any installers distributed after expiration
5. Update CI/CD secrets with new certificate

## File Structure

```
Certificates/
├── README.md                        # This file
├── .gitignore                       # Prevents certificates from being committed
├── WinClip-Test.pfx                # Test certificate (local only, not committed)
└── WinClip-Production-YYYY.pfx     # Production certificate (local only, not committed)
```
