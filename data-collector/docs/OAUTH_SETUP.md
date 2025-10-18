# ESI OAuth Setup Guide

This guide explains how to set up EVE Online ESI OAuth for character authentication.

## Prerequisites

- EVE Online account
- Access to https://developers.eveonline.com/

## Step 1: Create an ESI Application

1. Go to https://developers.eveonline.com/applications
2. Click "Create New Application"
3. Fill in the form:
   - **Application Name**: EVE Data Collector (or your preferred name)
   - **Description**: Data collector for EVE Online
   - **Connection Type**: Authentication & API Access
   - **Permissions/Scopes**: Select the scopes you need:
     - `esi-skills.read_skills.v1` - Read character skills
     - `esi-assets.read_assets.v1` - Read character assets
     - `esi-wallet.read_character_wallet.v1` - Read wallet
     - `esi-characters.read_corporation_roles.v1` - Read corp roles
   - **Callback URL**: `http://localhost:8080/callback`

4. Click "Create Application"
5. You will receive:
   - **Client ID**: A unique identifier for your application
   - **Secret Key**: A secret key (keep this secure!)

## Step 2: Configure the Application

1. Open `appsettings.json` in the `EveDataCollector.App` project
2. Update the `EsiOAuth` section with your credentials:

```json
{
  "EsiOAuth": {
    "ApplicationName": "EVE Data Collector",
    "ClientId": "YOUR_CLIENT_ID_HERE",
    "ClientSecret": "YOUR_SECRET_KEY_HERE",
    "CallbackUrl": "http://localhost:8080/callback",
    "Scopes": [
      "esi-skills.read_skills.v1",
      "esi-assets.read_assets.v1",
      "esi-wallet.read_character_wallet.v1",
      "esi-characters.read_corporation_roles.v1"
    ]
  }
}
```

**⚠️ IMPORTANT**: Never commit your `ClientSecret` to version control!

For production, use environment variables or a secure secrets manager:
- `EsiOAuth__ClientId`
- `EsiOAuth__ClientSecret`

## Step 3: Authorize a Character

1. Run the application:
   ```bash
   dotnet run --project src/EveDataCollector.App
   ```

2. Choose option "2" to authorize a new character

3. The application will:
   - Display an authorization URL
   - Open a temporary HTTP server on port 8080
   - Wait for you to complete the OAuth flow

4. Copy the URL and open it in your browser

5. Log in with your EVE Online account

6. Select the character you want to authorize

7. Review and accept the requested permissions

8. You will be redirected to `http://localhost:8080/callback`

9. The application will:
   - Exchange the authorization code for an access token
   - Fetch character information from ESI
   - Store the token and character in the database
   - Display a success message

## Step 4: Verify

Check the database to see your authorized character:

```sql
SELECT * FROM characters;
SELECT * FROM esi_tokens;
```

## Token Management

- **Access tokens** expire after 20 minutes
- The `TokenRefreshService` automatically refreshes tokens every 5 minutes
- **Refresh tokens** are long-lived and stored securely in the database
- If a token refresh fails, it will be marked as invalid

## Troubleshooting

### "Application not found" error
- Verify your `ClientId` and `ClientSecret` are correct
- Check that the application is active on developers.eveonline.com

### "Callback URL mismatch" error
- Ensure the callback URL in appsettings.json matches exactly with the one registered on developers.eveonline.com
- The URL is case-sensitive

### "Port already in use" error
- Change the port in the callback URL (both in appsettings.json and on developers.eveonline.com)
- Make sure no other application is using port 8080

### Token refresh failures
- Check the application logs for details
- Verify the refresh token is still valid
- You may need to re-authorize the character

## Security Best Practices

1. **Never share your Client Secret**
2. **Use environment variables** for production deployments
3. **Regularly rotate your Client Secret** on developers.eveonline.com
4. **Monitor token usage** and revoke unused tokens
5. **Use HTTPS** for production callback URLs
6. **Implement rate limiting** to avoid ESI throttling
7. **Encrypt tokens** in the database for production use
