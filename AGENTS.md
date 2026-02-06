# Repository Guidelines

## Project Overview
This project is a web-based system built with ASP.NET Core and MySQL to help restaurants manage production costs. It centralizes ingredient, recipe, and supplier management, automates cost calculations, supports profitability analysis, and provides comprehensive reporting features to keep operations efficient and data-driven.

## Page Responsibilities
- **Dashboard**
  - Surface key metrics such as total costs, profit, and most profitable dishes.
  - Offer category and date-range filters for at-a-glance insights.
  - Highlight alerts like ingredient price increases and allow rapid export of reports.
- **Ingredients**
  - List, create, update, and delete ingredient records with unit, category, and pricing details.
  - Associate ingredients with suppliers and maintain price history.
  - Support exporting ingredient data to CSV, Excel, or PDF.
- **Suppliers**
  - Manage the supplier directory, including contact data and CRUD operations.
  - Link suppliers to the ingredients they provide.
  - Provide export options for supplier information.
- **Recipes**
  - Display all recipes with categories and imagery.
  - Manage ingredient composition per recipe and keep automatic cost and suggested price calculations in sync.
  - Present profitability insights per recipe.
- **Recipe Form** (subpage of Recipes)
  - Add or modify recipe details, including uploading images.
  - Select ingredients, configure quantities, and set profit margins to derive suggested prices.
- **Price History**
  - Visualize historical ingredient price changes with filtering by ingredient or date.
  - Compare past and current prices and enable exports of the data set.
- **Settings**
  - Handle data import/export (CSV, Excel, PDF, and database backup).
  - Manage application preferences such as currency format, themes, and optional local password protection.
- **About** (optional)
  - Communicate system information, version details, legal notices, and support contacts.

## Development Instructions
- When implementing or modifying any user interface elements, always review `Prometheus_Brand_Identity.txt` to ensure the UI aligns with the defined brand identity.
- Maintain clear separation of concerns across the application's pages: each page should be responsible only for the functionality outlined above.
- Ensure export features (CSV, Excel, PDF) remain consistent and accessible across relevant pages.
- Preserve support for automated cost calculations, historical price tracking, and profitability reporting when making changes.

## Testing & Quality
- Verify that changes involving data import/export or calculations are covered by appropriate tests where possible.
- Before committing UI changes, double-check adherence to the branding guidelines defined in `Prometheus_Brand_Identity.txt`.
