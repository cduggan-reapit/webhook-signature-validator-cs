# Signature Tester Console App

A simple console application intended to make it easier to test webhook signature values.

## Usage

```cmd
Signature.exe test [-f filepath] [-e encoding] [-k key] [-s signature]
```

| Option        | Alias | Description                                                             |
|---------------|-------|-------------------------------------------------------------------------|
| `--file`      | `-f`  | The path of the utf-8 encoded text file containing the webhook payload. |
| `--encoding`  | `-e`  | The encoding used when preparing the public key (hex/base64).           |
| `--key`       | `-k`  | The encoded public key.                                                 |
| `--signature` | `-s`  | The signature value provided in the webhook request headers.            |

Any options not provided in the initial command will be requested during execution.

The application will exit with a code of `0` If the signature is found to be valid.
If the signature is invalid, the exit code will be `999`.

## Tips and Tricks

- Enter `^C` to exit at any time (`CTRL + C` on Windows)

### File

- We recommend using fully resolved file paths (e.g. "C:/.../directory/myFile.txt")
- If the file path contains spaces, make sure to wrap the value in quotation marks (`"`)
- The webhook payload is utf-8 encoded, it is important that the payload file is too

### Public Keys

- Values returned by the webhooks API (`/signing/{id}`) are encoded in a compatible `base64` format.
- When fetching public keys directly from MySQL/Aurora, it's easiest to export the values as `hex`.  This is because the 
  base64 encoded values returned from the API use a slightly different encoding scheme than the in-built `TO_BASE64` 
  function.
  ```sql
  SELECT HEX(publicKey) FROM signingkeys WHERE id = ?
  
  /* rather than */
  
  SELECT TO_BASE64(publicKey) FROM signingkeys WHERE id = ?
  ```
  
### Signature

- The signature should be in the format `s:guid:number:ed25519`
  - The guid is the unique identifier of the signing key
  - The number is the unix epoch timestamp of the signing operation, and is used as the payload object salt
  - The final element is the cryptographic signature, and the part that is actually being tested by this application.

## Build

- To create a single executable for the application, run the following command from the root directory:
  ```cmd
  # Powershell syntax, remove the leading .\ from the file path for bash & cmdshell environments
  dotnet publish .\src\Signature\Signature.csproj -r win-x64 -c Release -o publish /p:PublishSingleFile=true
  ```
  
