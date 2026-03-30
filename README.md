# Prerequisites

Make sure the following tools are installed on your system:

Docker (including Docker Compose)

Node.js & npm (required for the Angular frontend)


# Running the Project¨
## !Run the start.bat in the semesterprojekt and if the docker and anuglar frontend runs then skip this "running the project"!
1. Start Backend Services with Docker
2. From the root semesterprojekt folder, run: docker-compose up --build
3. This will build and start all backend services, including:
Order Service
Warehouse Service
PostgreSQL
RabbitMQ
Prometheus

4. Start the Angular Frontend by opening a new terminal
5. Navigate to the Angular frontend directory:
6. cd to AngularFrontend folder
7. Start the frontend: npm start
8. Open the browser using the port shown in the terminal (usually: http://127.0.0.1:63028/ or http://localhost:63028/)

# Using the Application
1. Create an account by register a new user in the frontend.
2. Password requirements:
At least one uppercase letter
At least one number
At least one symbol

3. Sell a book by navigate to the Sell page (top right corner).
4. Follow the selling steps using this ISBN number for testing "9781338878929" This ISBN corresponds to Harry Potter.

# Using the OrderService
Testing the Order Service by Swagger
Swagger UI URL: http://localhost:8080/swagger

1. Create an Order (POST)
2. Use the POST endpoint in Swagger and send the following JSON body
3. fill in the value of bookId you get from calling 'curl http://localhost:5001/warehouse/books' which gives you all the books:
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "items": [
    {
      "bookId": "",
      "quantity": 1,
      "condition": "",
      "price": 0
    }
  ]
}


4. After submitting you will receive a response containing an orderId

# Check Order Status (GET)

4. Copy the returned orderId
5. Use the GET endpoint in Swagger
6. If everything is working correctly, the order status should be: Validated

# Service Access & Ports
## RabbitMQ

URL: http://localhost:15672

Username: appuser

Password: apppassword

## Prometheus

URL: http://localhost:9090/query

PostgreSQL Database

Port: 5234

Username: postgres

Password: postgrespw

Databases: ordersdb, warehouse