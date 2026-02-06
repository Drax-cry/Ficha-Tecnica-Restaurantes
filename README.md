Project Description

A web-based system built with ASP.NET Core and MySQL, designed to help restaurants manage production costs efficiently.
The platform will enable full control of ingredients, recipes, and suppliers, automatically calculate product costs and suggested prices, track price history, and generate profitability reports.

The system will include dedicated pages with clear responsibilities to keep the structure organized, scalable, and easy to use.

📄 Main Pages and Responsibilities
🏠 Dashboard Page

Display key performance metrics (total costs, total profit, most profitable dishes)

Filter reports by category or date range

Show alerts (e.g., ingredient price increase)

Quick access to export reports

🧾 Ingredients Page

List, add, edit, and delete ingredients

Define unit of measurement, category, and price

Link ingredients to suppliers

View and manage ingredient price history

Export ingredient data (CSV, Excel, PDF)

🏷️ Suppliers Page

Manage supplier list (create, edit, delete)

Store contact information

Link suppliers to ingredients

Export supplier list

🍽️ Recipes Page

List all recipes with images and categories

Add, edit, or delete recipes

Manage ingredient quantities within each recipe

Automatically calculate total recipe cost and suggested price

View recipe profitability

✏️ Recipe Form Page (subpage of Recipes)

Add or update recipe details

Upload recipe image

Select ingredients and define quantities

Set profit margin and view suggested selling price

⏳ Price History Page

Display historical price changes for ingredients

Filter by date or ingredient

Compare old vs. new prices

Export price history data

⚙️ Settings Page

Import and export data (CSV, Excel, PDF, database backup)

Optional local password protection

Manage application preferences (e.g., currency format, themes)

ℹ️ About Page (optional)

Display system and version information

Support contact details

Legal information (e.g., Terms of Use)

⚙️ Key Features

Ingredient/product management

Recipe management with photos and categories

Supplier management

Automatic recipe cost calculation

Suggested selling price with configurable profit margin

Ingredient price history tracking

Technical sheet calculation per portion

Dashboard with top profitable dishes

Dashboard filters by category or time period

Automatic recipe cost updates when ingredient prices change

Profitability analysis by recipe or ingredient

Data import and export (CSV, Excel, PDF)

Centralized storage with MySQL

Scalable architecture with multi-user support

🛠️ Database Setup

To create the MySQL database and the initial `login` table for the application, run the following command in your MySQL client:

```sql
SOURCE database/setup_login_db.sql;
```

This script creates the `ficha_tecnica_restaurantes` database (if it does not already exist) and provisions a `login` table with unique constraints for both username and email fields. You can adapt the script to integrate with your preferred authentication workflow as needed.

> 💡 **Upgrading existing databases**
>
> If your environment was provisioned before support for very high recipe margins was added, run `database/migrations_expand_target_margin.sql` against the existing schema. The script increases the precision of the `recipes.target_margin` column so margins above 500% no longer trigger out-of-range errors when saving recipes.

🔧 Configuration

Supply the database connection string through configuration rather than editing `appsettings.json` directly. The application first reads `ConnectionStrings:DefaultConnection` (e.g., via `appsettings.Development.json` or `appsettings.Production.json`) and then falls back to a `DB_CONNECTION` environment variable. When deploying to Google Cloud, store the full connection string in Secret Manager and inject it as `DB_CONNECTION`. For local development you can use [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) or a `.env` file that defines the same variable. Ensure the string keeps TLS enabled (e.g., `SslMode=Required`) when using IAM database authentication, because the `mysql_clear_password` plugin demands an encrypted channel.

To enable Google Cloud Logging, provide the project identifier through configuration in one of two ways:

1. **Configuration file** – set `GoogleCloud:ProjectId` in the applicable settings file, such as `Ficha Tecnica/appsettings.Development.json` for local development or `Ficha Tecnica/appsettings.json` for production defaults.
2. **Environment variable** – set `GOOGLE_CLOUD_PROJECT` (or `GOOGLE_PROJECT_ID`) in the hosting environment. This is useful for containerized deployments or Cloud Run jobs where environment variables are easier to manage than files.

Once the project id is detected at startup the app automatically wires Google Diagnostics and directs logs to Cloud Logging using the configured log name.

To trigger a server connectivity check during startup, configure a probe URL through `ServerConnectionProbe:Url` (for example in `Ficha Tecnica/appsettings.Production.json`) or the `SERVER_PROBE_URL` environment variable. The application performs a single GET request after it launches and logs the result—including timing, status code, and any response summary—through the configured logging pipeline (e.g., Google Cloud Logging when enabled).

🧰 Debugging with Cloud SQL (IAM Service Account)

When debugging locally against the production Cloud SQL instance, use the Cloud SQL Auth Proxy v2 with IAM authentication. The high-level workflow is:

1. **Download tooling**
   - Install the [Google Cloud CLI](https://cloud.google.com/sdk/docs/install) and authenticate with `gcloud auth login`.
   - Download the Cloud SQL Auth Proxy binary and place it in a folder on your `PATH` (for example `C:\tools`).
   - Create a service-account JSON key with the `Cloud SQL Client` role and store it securely (e.g., `C:\Users\<you>\Secrets\cloud-sql.json`).

2. **Start the proxy**
   ```powershell
   $env:GOOGLE_APPLICATION_CREDENTIALS = "C:\Users\<you>\Secrets\cloud-sql.json"
   cloud_sql_proxy.exe --port 3307 --auto-iam-authn modified-argon-476211-j9:europe-west1:restaurant-services
   ```
   Leave the window running; the proxy listens on `127.0.0.1:3307` and exchanges IAM tokens on your behalf.

3. **Launch Visual Studio from the same environment**
   ```powershell
   $env:GOOGLE_APPLICATION_CREDENTIALS = "C:\Users\<you>\Secrets\cloud-sql.json"
   & "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe"
   ```

4. **Set the connection string**
   Use the loopback endpoint exposed by the proxy. Cloud SQL shortens MySQL usernames to 32 characters, so the IAM database user appears as the service-account name without the domain. You can confirm the exact username with `gcloud sql users list --instance=restaurant-services`. Example connection string:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=127.0.0.1;Port=3307;Database=<DB_NAME>;Uid=ft-prod-access;Pwd=;"
     }
   }
   ```
   Grant privileges inside MySQL using the same truncated username:
   ```sql
   GRANT ALL PRIVILEGES ON <DB_NAME>.* TO 'ft-prod-access';
   FLUSH PRIVILEGES;
   ```

5. **Debug normally**
   Press **F5** in Visual Studio. Watch the proxy console for authentication messages. When you finish debugging, stop the proxy with `Ctrl+C`.

> ℹ️ The Cloud SQL Auth Proxy requires the [Cloud SQL Admin API](https://console.cloud.google.com/apis/api/sqladmin.googleapis.com) to be enabled in the Google Cloud project. Enable it once and wait a few minutes before retrying the proxy if you see a `SERVICE_DISABLED` error.

🧪 Troubleshooting Ingredient Inserts

If you encounter errors while inserting ingredients, note that the database schema allows the `supplier` field to be null and it
is not linked to a suppliers table. You can therefore create ingredients even when no suppliers exist yet. Most insert errors ar
e typically caused by required fields such as `name`, `unit`, `cost_per_unit`, or the `user_id` foreign key not being provided, o
r by attempting to reuse the same ingredient name for the same user (which violates the `ux_ingredients_user_name` unique constra
int).
