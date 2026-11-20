# OpenAPI and Swagger

Development URLs:

- [Swagger UI](http://localhost:5080/swagger)
- [OpenAPI JSON](http://localhost:5080/openapi/v1.json)

The native ASP.NET Core OpenAPI generator produces the document; Swagger UI renders it.

To call protected endpoints:

1. Call `POST /api/auth/login` using a development account.
2. Copy the returned session token.
3. Select **Authorize** and paste only the token.
4. Execute the protected request.

Swagger sends `Authorization: Bearer <token>`.

The token is an opaque session token, not a JWT. API sessions are stored in memory, expire according to configuration and are lost when the API restarts. Swagger authorization persistence is not enabled.

Public endpoints do not require a token. Protected operations enforce their actual roles and ownership rules in the API; Swagger descriptions do not replace those checks.

Do not include real passwords or tokens in examples, screenshots or commits.
