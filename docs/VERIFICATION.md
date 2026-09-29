# Verification

Verified in a Linux environment with .NET SDK 10.0.401.

- Release solution build: **passed**, 0 warnings, 0 errors.
- Executable regression suite: **8 passed**.
- HTTP sign-in and rendered-page checks: **26 routes passed** across Admin, Manager and Staff.
- Unauthorized role access: **denied**.
- POST without anti-forgery token: **rejected with HTTP 400**.
- Donation completion through MVC forms: **passed**; donation and stock updated.
- Stock issue through MVC forms: **passed**.
- FAQ, donor, event and blood-need creation: **passed**.
- Bank-registration submission and administrator approval: **passed**.
- Manager invitation, token-based password setting, manager sign-in and staff invitation: **passed** using the local development outbox.

## Regression cases

1. Completing an open appointment creates one donation and one stock batch.
2. Repeated completion cannot double-count stock.
3. Another bank cannot complete the appointment.
4. A pending appointment cannot be completed directly.
5. Stock issues use earliest expiry and exclude expired/other-bank stock.
6. Insufficient stock leaves quantities and issue history unchanged.
7. Invalid quantities and blood groups are rejected.
8. Expired donations cannot create usable stock.

## Visual verification

Razor views compiled and the rendered HTML routes passed HTTP checks. A browser screenshot pass could not be completed: the available browser download was truncated in this environment. Visual layout still needs a local browser review.

## Not verified here

Live SMTP delivery, production hosting, Windows-specific execution and migration of the original Flask database. No real donor data was used.
