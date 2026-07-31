# API Reference

This document provides detailed information about the NieTemplate API endpoints.

## Base URL

- **Development**: `https://localhost:5001/api/v1`
- **Production**: `https://api.yourcompany.com/api/v1`

## Authentication

All API endpoints (except health checks) require session-based authentication.

### Headers

| Header         | Required | Description                              |
| -------------- | -------- | ---------------------------------------- |
| `X-Session-Id` | Yes      | Session token obtained from Auth service |
| `Content-Type` | Yes      | `application/json` for JSON payloads     |

### Error Responses

| Status Code               | Description                |
| ------------------------- | -------------------------- |
| 401 Unauthorized          | Invalid or expired session |
| 403 Forbidden             | Insufficient permissions   |
| 404 Not Found             | Resource not found         |
| 500 Internal Server Error | Server error               |

---

## Health Check

### GET /health

Check the health status of the API and its dependencies.

**Authentication Required**: No

**Response**

```json
{
  "status": "Healthy",
  "entries": {
    "postgresql": {
      "status": "Healthy",
      "duration": "00:00:00.0234567"
    },
    "valkey": {
      "status": "Healthy",
      "duration": "00:00:00.0012345"
    }
  }
}
```

---

## Code Controller

Manages lookup/reference data codes.

### GET /api/v1/code/getall

Retrieves all codes.

**Response**

```json
[
  {
    "id": "1",
    "type": "STATUS",
    "code": "ACTIVE",
    "description": "Active status",
    "sortOrder": 1
  }
]
```

### GET /api/v1/code/getbytype/{type}

Retrieves codes by type.

**Parameters**
| Name | Type | Required | Description |
|------|------|----------|-------------|
| type | string | Yes | Code type (e.g., "STATUS", "CATEGORY") |

**Response**

```json
[
  {
    "id": "1",
    "type": "STATUS",
    "code": "ACTIVE",
    "description": "Active status",
    "sortOrder": 1
  }
]
```

---

## Sample Model Controller

Example CRUD operations for the SampleModel entity.

### GET /api/v1/samplemodel/getall

Retrieves all sample models.

**Response**

```json
[
  {
    "id": 1,
    "mandatoryField": "Test",
    "nonMandatoryField": null,
    "sampleEnum": "Option1",
    "childModels": [
      {
        "id": 1,
        "name": "Child 1"
      }
    ]
  }
]
```

### GET /api/v1/samplemodel/getbyid/{id}

Retrieves a sample model by ID.

**Parameters**
| Name | Type | Required | Description |
|------|------|----------|-------------|
| id | integer | Yes | Sample model ID |

**Response**

```json
{
  "id": 1,
  "mandatoryField": "Test",
  "nonMandatoryField": null,
  "sampleEnum": "Option1",
  "childModels": []
}
```

### POST /api/v1/samplemodel/create

Creates a new sample model.

**Request Body**

```json
{
  "mandatoryField": "New Sample",
  "nonMandatoryField": "Optional value",
  "sampleEnum": "Option1"
}
```

**Response**

```json
{
  "id": 1,
  "mandatoryField": "New Sample",
  "nonMandatoryField": "Optional value",
  "sampleEnum": "Option1"
}
```

### PUT /api/v1/samplemodel/update

Updates an existing sample model.

**Request Body**

```json
{
  "id": 1,
  "mandatoryField": "Updated Sample",
  "nonMandatoryField": "Updated value",
  "sampleEnum": "Option2"
}
```

**Response**: 204 No Content

### DELETE /api/v1/samplemodel/delete/{id}

Deletes a sample model.

**Parameters**
| Name | Type | Required | Description |
|------|------|----------|-------------|
| id | integer | Yes | Sample model ID |

**Response**: 204 No Content

---

## Document Controller

Manages file uploads and downloads.

### GET /api/v1/document/downloadfile/{id}

Downloads a document by ID.

**Parameters**
| Name | Type | Required | Description |
|------|------|----------|-------------|
| id | integer | Yes | Document ID |

**Response**: File stream with appropriate content type

### POST /api/v1/document/uploadfile

Uploads a new document.

**Request**: `multipart/form-data`
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| file | file | Yes | File to upload |
| sampleModelId | integer | No | Associated sample model ID |

**Response**

```json
{
  "id": 1,
  "filePath": "/2024-01/abc123.pdf",
  "userFileName": "document.pdf",
  "fileSize": 12345
}
```

### DELETE /api/v1/document/deletefile/{id}

Deletes a document.

**Parameters**
| Name | Type | Required | Description |
|------|------|----------|-------------|
| id | integer | Yes | Document ID |

**Response**: 204 No Content

---

## Error Response Format

All API errors follow this format:

```json
{
  "statusCode": 400,
  "message": "Error message description",
  "details": "Additional details (development only)"
}
```

---

## Rate Limiting

Currently, no rate limiting is implemented. For production deployments, consider adding:

- Azure API Management
- YARP reverse proxy with rate limiting
- Custom middleware

---

## Versioning

The API uses URL-based versioning:

- Current version: `v1`
- Version header: `X-Api-Version` (alternative)

Example:

```
GET /api/v1/samplemodel/getall
GET /api/v2/samplemodel/getall  (future)
```

