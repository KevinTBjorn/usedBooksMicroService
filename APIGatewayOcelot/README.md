# API Gateway (Ocelot)

Dette er API Gateway'en for systemet, bygget med Ocelot.

## Konfiguration

Gateway'en bruger `ocelot.json` til at konfigurere routing til microservices.

### Routing

- `/auth/register` ? AuthService
- `/store/listings/preview` ? AddBookService (preview)
- `/store/listings` ? AddBookService (create)
- `/store/books` ? WarehouseService (paginated books)
- `/store/userbooks/{bookId}` ? WarehouseService (listings for a book)
- `/store/userbooks/user/{userId}` ? WarehouseService (user's listings)

### CORS

Gateway'en tillader requests fra:
- `http://localhost:63028` (Angular dev)
- `http://localhost:4200` (Angular standard)
- `http://127.0.0.1:63028`
- `http://127.0.0.1:4200`
- `http://localhost:5139` (for testing gateway direkte)

## Docker

Gateway'en køres i Docker og eksponerer port 5139 til host:

```bash
# Start alle services
docker-compose up -d

# Se gateway logs
docker-compose logs -f apigateway

# Stop services
docker-compose down
```

Gateway'en kommunikerer internt med services via Docker network `gtlib-net`.

## Port Mapping

| Service | Container Port | Host Port |
|---------|---------------|-----------|
| API Gateway | 8080 | 5139 |
| AuthService | 8080 | 7001 |
| WarehouseService | 80 | 5001 |
| AddBookService | 8080 | 5030 |

## Test Gateway

```bash
# Health check
curl http://localhost:5139/health

# Test warehouse route
curl "http://localhost:5139/store/books?pageNumber=1&pageSize=10"

# Test with CORS (fra Angular)
# Angular skal kalde: http://localhost:5139/store/books
```

## Troubleshooting

### CORS fejl
- Gateway håndterer OPTIONS requests automatisk
- Alle routes accepterer både GET/POST/DELETE og OPTIONS
- CORS middleware er aktiveret før Ocelot

### 404 fejl
- Check at downstream services kører: `docker-compose ps`
- Check logs: `docker-compose logs [service-name]`
- Verificer at port mappings matcher `ocelot.json`

### Connection refused
- Services skal være på samme Docker network (`gtlib-net`)
- Brug service navn (ikke localhost) i `ocelot.json`
- WarehouseService bruger port 80 (ikke 8080)
