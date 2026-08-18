# VEMT license-key generator

This is a vendor-only offline utility. It signs a VEMT license payload with the RSA private key and produces the `payload.signature` token accepted by `LicenseService`.

Keep the private PEM and this utility away from the customer installation and installer. The customer application receives only the public PEM embedded in `LicenseService` and the generated product key.

Use the Installation ID shown on the customer's activation screen:

```cmd
dotnet run --project Tools/LicenseKeyGenerator -- --private-key C:\VEMT-License\VEMT_private.pem --installation-id 1443b4f3913e45fa97ed501a0a10da5f --type Lifetime
```

For a one-year key:

```cmd
dotnet run --project Tools/LicenseKeyGenerator -- --private-key C:\VEMT-License\VEMT_private.pem --installation-id 1443b4f3913e45fa97ed501a0a10da5f --type Annual
```

The command prints one product-key line. Copy that entire line into the activation screen's Product key field.
