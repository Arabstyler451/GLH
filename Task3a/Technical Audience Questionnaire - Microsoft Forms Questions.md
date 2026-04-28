# Technical Audience Questionnaire - Microsoft Forms Questions

Purpose: collect technical feedback on the Greenfield Local Hub ASP.NET Core MVC web application. The questions below are written for a technical audience and are split into sections that can be copied into Microsoft Forms.

Suggested answer scale for rating questions: 1 = Very poor, 2 = Poor, 3 = Acceptable, 4 = Good, 5 = Excellent.

## Section 1 - Technical Background

| No. | Question | Required | Microsoft Forms type | Suggested options or scale |
| --- | --- | --- | --- | --- |
| 1 | What best describes your technical background? | Yes | Choice (single select) | Student developer; Web developer; Software engineer; Database developer; Tester/QA; Teacher/assessor; Other |
| 2 | How familiar are you with ASP.NET Core MVC? | Yes | Rating | 1-5 |
| 3 | How familiar are you with Entity Framework Core? | Yes | Rating | 1-5 |
| 4 | How familiar are you with ASP.NET Core Identity and role-based access? | Yes | Rating | 1-5 |
| 5 | Have you reviewed or tested an MVC web application before? | Yes | Choice (single select) | Yes; No; Not sure |
| 6 | Which areas of the web application did you review? | Yes | Choice (multi-select) | Code structure; Models; Views; Controllers; Database; Security; User interface; Checkout; Producer dashboard; Accessibility; Testing |
| 7 | How confident are you in the feedback you are giving? | Yes | Rating | 1-5 |
| 8 | Is there any technical context about your background that may affect your feedback? | No | Text (long answer) | Open response |

## Section 2 - Overall Architecture

| No. | Question | Required | Microsoft Forms type | Suggested options or scale |
| --- | --- | --- | --- | --- |
| 9 | How clear is the overall MVC structure of the project? | Yes | Rating | 1-5 |
| 10 | Does the project separate responsibilities clearly between models, views and controllers? | Yes | Choice (single select) | Yes; Mostly; Partly; No; Not sure |
| 11 | How suitable is ASP.NET Core MVC for this web application? | Yes | Rating | 1-5 |
| 12 | How easy was it to understand the purpose of the application from the code structure? | Yes | Rating | 1-5 |
| 13 | Are the main project folders named and organised in a clear way? | Yes | Choice (single select) | Yes; Mostly; Partly; No |
| 14 | Which part of the architecture seems strongest? | No | Text (long answer) | Open response |
| 15 | Which part of the architecture seems weakest or most confusing? | No | Text (long answer) | Open response |
| 16 | How easy would it be for another developer to add a new feature to this project? | Yes | Rating | 1-5 |
| 17 | Does the project appear to follow common ASP.NET Core conventions? | Yes | Choice (single select) | Yes; Mostly; Partly; No; Not sure |
| 18 | What architectural improvement would have the biggest positive impact? | No | Text (long answer) | Open response |

## Section 3 - Models and Database Design

| No. | Question | Required | Microsoft Forms type | Suggested options or scale |
| --- | --- | --- | --- | --- |
| 19 | How clear are the model classes used for products, producers, orders and carts? | Yes | Rating | 1-5 |
| 20 | Are the relationships between products, producers, categories, carts and orders easy to understand? | Yes | Rating | 1-5 |
| 21 | Does the database design support the main user journeys of the website? | Yes | Choice (single select) | Yes; Mostly; Partly; No; Not sure |
| 22 | How suitable are the model names and property names? | Yes | Rating | 1-5 |
| 23 | Are there any model names or property names that should be renamed for clarity? | No | Text (long answer) | Open response |
| 24 | How well does the product model support a local food catalogue? | Yes | Rating | 1-5 |
| 25 | How well does the producer model support information about local suppliers? | Yes | Rating | 1-5 |
| 26 | How well does the order model support checkout and order history? | Yes | Rating | 1-5 |
| 27 | How well does the shopping cart model support adding, removing and changing quantities? | Yes | Rating | 1-5 |
| 28 | How well does the loyalty account model support the rewards feature? | Yes | Rating | 1-5 |
| 29 | Should prices and totals use decimal instead of float for money values? | Yes | Choice (single select) | Yes; No; Not sure |
| 30 | Are validation attributes used well enough on the model properties? | Yes | Rating | 1-5 |
| 31 | Which model needs the most improvement? | Yes | Choice (single select) | Products; Producers; Orders; Order products; Shopping cart; Addresses; Loyalty account; Categories; Other |
| 32 | What specific model or database improvement would you recommend first? | No | Text (long answer) | Open response |
| 33 | Are there any database fields that appear unnecessary or missing? | No | Text (long answer) | Open response |
| 34 | How confident are you that the database can handle future features? | Yes | Rating | 1-5 |

## Section 4 - Controllers and Business Logic

| No. | Question | Required | Microsoft Forms type | Suggested options or scale |
| --- | --- | --- | --- | --- |
| 35 | How clear is the logic inside the main controllers? | Yes | Rating | 1-5 |
| 36 | Do the controllers keep a suitable balance between page logic and business logic? | Yes | Rating | 1-5 |
| 37 | How easy is it to follow the product browsing flow in the products controller? | Yes | Rating | 1-5 |
| 38 | How easy is it to follow the basket and cart logic? | Yes | Rating | 1-5 |
| 39 | How easy is it to follow the checkout logic in the orders controller? | Yes | Rating | 1-5 |
| 40 | How easy is it to follow the producer dashboard logic? | Yes | Rating | 1-5 |
| 41 | Are controller actions named clearly enough? | Yes | Choice (single select) | Yes; Mostly; Partly; No |
| 42 | Are there controller actions that contain too much code and should be simplified? | Yes | Choice (single select) | Yes; No; Not sure |
| 43 | If yes, which controller actions should be simplified? | No | Text (long answer) | Open response |
| 44 | How well are database queries used inside the controllers? | Yes | Rating | 1-5 |
| 45 | Are includes and related data loaded in a clear and suitable way? | Yes | Rating | 1-5 |
| 46 | How well does the application handle missing records or invalid IDs? | Yes | Rating | 1-5 |
| 47 | How well does the application prevent users from accessing data that is not theirs? | Yes | Rating | 1-5 |
| 48 | Should more logic be moved into services instead of staying inside controllers? | Yes | Choice (single select) | Yes; No; Not sure |
| 49 | What controller or business logic issue should be fixed first? | No | Text (long answer) | Open response |

## Section 5 - Views, Razor Pages and UI

| No. | Question | Required | Microsoft Forms type | Suggested options or scale |
| --- | --- | --- | --- | --- |
| 50 | How clear and consistent are the Razor views? | Yes | Rating | 1-5 |
| 51 | How suitable is the layout for a local food hub website? | Yes | Rating | 1-5 |
| 52 | How easy is it to navigate between the main pages? | Yes | Rating | 1-5 |
| 53 | How effective is the product catalogue layout? | Yes | Rating | 1-5 |
| 54 | How effective is the product details page? | Yes | Rating | 1-5 |
| 55 | How effective are the producer profile pages? | Yes | Rating | 1-5 |
| 56 | How effective is the shopping basket page? | Yes | Rating | 1-5 |
| 57 | How effective is the checkout page layout? | Yes | Rating | 1-5 |
| 58 | How effective is the producer dashboard layout? | Yes | Rating | 1-5 |
| 59 | How clear are the forms for creating or editing products? | Yes | Rating | 1-5 |
| 60 | Are labels, buttons and headings written clearly for users? | Yes | Choice (single select) | Yes; Mostly; Partly; No |
| 61 | Are there any pages where the UI feels unfinished or confusing? | No | Text (long answer) | Open response |
| 62 | How consistent is the styling across public pages and account pages? | Yes | Rating | 1-5 |
| 63 | How well does the site work visually as a complete product? | Yes | Rating | 1-5 |
| 64 | What view or page should be improved first? | No | Text (long answer) | Open response |

## Section 6 - Authentication, Roles and User Accounts

| No. | Question | Required | Microsoft Forms type | Suggested options or scale |
| --- | --- | --- | --- | --- |
| 65 | How well does the website separate user, producer and admin responsibilities? | Yes | Rating | 1-5 |
| 66 | Are the Identity login and register pages suitable for the website? | Yes | Rating | 1-5 |
| 67 | How clear is the account management area? | Yes | Rating | 1-5 |
| 68 | Are role restrictions applied consistently across the application? | Yes | Choice (single select) | Yes; Mostly; Partly; No; Not sure |
| 69 | Are there any pages that should require login but appear publicly accessible? | No | Text (long answer) | Open response |
| 70 | Are there any pages that should be restricted to admin or producer users only? | No | Text (long answer) | Open response |
| 71 | How serious is the missing authentication middleware issue if it is not fixed? | Yes | Rating | 1-5 |
| 72 | How well does the application protect customer account information? | Yes | Rating | 1-5 |
| 73 | How well does the application protect producer-only features? | Yes | Rating | 1-5 |
| 74 | What is the most important account or role improvement needed? | No | Text (long answer) | Open response |

## Section 7 - Security and Data Protection

| No. | Question | Required | Microsoft Forms type | Suggested options or scale |
| --- | --- | --- | --- | --- |
| 75 | How secure does the web application appear overall? | Yes | Rating | 1-5 |
| 76 | How serious is storing a Google OAuth client secret in appsettings.json? | Yes | Rating | 1-5 |
| 77 | Should secrets be moved to user secrets or environment variables? | Yes | Choice (single select) | Yes; No; Not sure |
| 78 | How well does the application protect order details from being viewed by the wrong user? | Yes | Rating | 1-5 |
| 79 | How well does the application protect address details from being viewed by the wrong user? | Yes | Rating | 1-5 |
| 80 | How well does the application protect producer dashboard data from unauthorised users? | Yes | Rating | 1-5 |
| 81 | Are delete and edit actions protected well enough? | Yes | Choice (single select) | Yes; Mostly; Partly; No; Not sure |
| 82 | Does the site appear to handle anti-forgery protection correctly on forms? | Yes | Choice (single select) | Yes; Mostly; Partly; No; Not sure |
| 83 | Are error messages safe and not exposing too much technical detail to users? | Yes | Rating | 1-5 |
| 84 | What security issue should be treated as the highest priority? | No | Text (long answer) | Open response |
| 85 | What security test would you recommend carrying out first? | No | Text (long answer) | Open response |
| 86 | How confident would you be using this site with real customer data after the identified fixes are made? | Yes | Rating | 1-5 |

## Section 8 - Shopping Basket, Checkout and Orders

| No. | Question | Required | Microsoft Forms type | Suggested options or scale |
| --- | --- | --- | --- | --- |
| 87 | How clear is the basket workflow from product page to checkout? | Yes | Rating | 1-5 |
| 88 | How well does the basket handle quantity changes? | Yes | Rating | 1-5 |
| 89 | How well does the checkout process match what users would expect? | Yes | Rating | 1-5 |
| 90 | How clear is the address selection or address entry process? | Yes | Rating | 1-5 |
| 91 | How well does the checkout process validate stock before completing an order? | Yes | Rating | 1-5 |
| 92 | How serious is saving an order before final stock validation? | Yes | Rating | 1-5 |
| 93 | How clear is the order confirmation or order history experience? | Yes | Rating | 1-5 |
| 94 | How well are delivery fee and total amount calculations presented? | Yes | Rating | 1-5 |
| 95 | Are there any checkout steps that should be made clearer or safer? | No | Text (long answer) | Open response |
| 96 | What is the biggest technical risk in the basket or checkout flow? | No | Text (long answer) | Open response |
| 97 | What checkout improvement would most improve user trust? | No | Text (long answer) | Open response |

## Section 9 - Producer Features and Product Management

| No. | Question | Required | Microsoft Forms type | Suggested options or scale |
| --- | --- | --- | --- | --- |
| 98 | How useful is the producer dashboard for managing a local supplier account? | Yes | Rating | 1-5 |
| 99 | How clear are the dashboard tabs, filters or summary information? | Yes | Rating | 1-5 |
| 100 | How well does the product create/edit flow support producers? | Yes | Rating | 1-5 |
| 101 | How well does the dashboard support stock management? | Yes | Rating | 1-5 |
| 102 | How well does the dashboard support viewing and updating orders? | Yes | Rating | 1-5 |
| 103 | Are producer users prevented from managing products that do not belong to them? | Yes | Choice (single select) | Yes; Mostly; Partly; No; Not sure |
| 104 | What producer feature needs the most improvement? | No | Text (long answer) | Open response |
| 105 | What extra feature would make the dashboard more useful for producers? | No | Text (long answer) | Open response |

## Section 10 - Accessibility and Front-End Behaviour

| No. | Question | Required | Microsoft Forms type | Suggested options or scale |
| --- | --- | --- | --- | --- |
| 106 | How useful are the dark mode and high contrast features? | Yes | Rating | 1-5 |
| 107 | How useful are the text size controls? | Yes | Rating | 1-5 |
| 108 | How useful is the read page or text-to-speech feature? | Yes | Rating | 1-5 |
| 109 | How well do the accessibility controls fit into the site design? | Yes | Rating | 1-5 |
| 110 | How well does the website support keyboard navigation? | Yes | Rating | 1-5 |
| 111 | How clear are hover, focus and active states on buttons and links? | Yes | Rating | 1-5 |
| 112 | How responsive does the site appear on smaller screens? | Yes | Rating | 1-5 |
| 113 | Are there any front-end scripts that seem unnecessary or unreliable? | No | Text (long answer) | Open response |
| 114 | What accessibility improvement would have the biggest impact? | No | Text (long answer) | Open response |

## Section 11 - Validation, Error Handling and Reliability

| No. | Question | Required | Microsoft Forms type | Suggested options or scale |
| --- | --- | --- | --- | --- |
| 115 | How well are user inputs validated across forms? | Yes | Rating | 1-5 |
| 116 | How clear are validation messages for users? | Yes | Rating | 1-5 |
| 117 | How well does the site handle invalid product, order or address IDs? | Yes | Rating | 1-5 |
| 118 | How well does the site recover from common user mistakes? | Yes | Rating | 1-5 |
| 119 | How well does the site handle empty baskets or missing checkout data? | Yes | Rating | 1-5 |
| 120 | Does the postcode lookup/validation behaviour seem reliable enough? | Yes | Rating | 1-5 |
| 121 | What validation rule should be added or improved first? | No | Text (long answer) | Open response |
| 122 | What reliability issue would you test first? | No | Text (long answer) | Open response |

## Section 12 - Testing, Maintainability and Future Improvements

| No. | Question | Required | Microsoft Forms type | Suggested options or scale |
| --- | --- | --- | --- | --- |
| 123 | How maintainable is the project in its current state? | Yes | Rating | 1-5 |
| 124 | How easy would it be to write tests for the main features? | Yes | Rating | 1-5 |
| 125 | Which feature should have automated tests first? | Yes | Choice (single select) | Login and roles; Product catalogue; Basket; Checkout; Orders; Producer dashboard; Address management; Loyalty rewards |
| 126 | How important is adding tests before further development? | Yes | Rating | 1-5 |
| 127 | How clear are the seeded demo users and sample data? | Yes | Rating | 1-5 |
| 128 | How suitable is the use of LocalDB for this development version? | Yes | Rating | 1-5 |
| 129 | How easy would it be to deploy this project after security fixes? | Yes | Rating | 1-5 |
| 130 | Which technical improvement should be done first? | Yes | Choice (single select) | Add authentication middleware; Move secrets out of appsettings; Fix role/ownership checks; Improve checkout stock validation; Change money fields to decimal; Add tests; Improve validation; Fix text encoding issues |
| 131 | Please explain why you selected that as the first improvement. | Yes | Text (long answer) | Open response |
| 132 | What is the strongest technical part of the project? | No | Text (long answer) | Open response |
| 133 | What is the weakest technical part of the project? | No | Text (long answer) | Open response |
| 134 | What one change would most improve the website for real users? | Yes | Text (long answer) | Open response |
| 135 | Any final technical feedback or recommendations? | No | Text (long answer) | Open response |

