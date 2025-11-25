# ASQL Quick Reference Guide

## What is ASQL?

**ASQL (Auction Search Query Language)** is a simple yet powerful query language for filtering products and bids in the Auction API.

---

## Operators

| Operator | Description | Example |
|----------|-------------|---------|
| `=` | Equal | `productId=1` |
| `!=` | Not equal | `status!="Expired"` |
| `<` | Less than | `price<1000` |
| `<=` | Less than or equal | `price<=500` |
| `>` | Greater than | `price>100` |
| `>=` | Greater than or equal | `price>=1000` |
| `in` | In array | `category in ["Art", "Fashion"]` |

## Logical Operators

| Operator | Description | Example |
|----------|-------------|---------|
| `AND` | Both conditions | `category="Art" AND price>=1000` |
| `OR` | Either condition | `id=1 OR id=2` |

---

## Product Fields

```
productId       (int)       - Product identifier
name            (string)    - Product name
category        (string)    - Product category
startingPrice   (decimal)   - Starting bid price
auctionDuration (int)       - Auction duration in minutes
status          (string)    - Auction status
```

## Bid Fields

```
bidderId    (int)       - User who placed bid
productId   (int)       - Product being bid on
amount      (decimal)   - Bid amount
timestamp   (DateTime)  - When bid was placed
```

---

## Quick Examples

### Products

```bash
# Single product
?asql=productId=1

# By name
?asql=name="Vintage Watch"

# By category
?asql=category="Electronics"

# Price range
?asql=startingPrice>=100 AND startingPrice<=1000

# Multiple categories
?asql=category in ["Art", "Electronics", "Fashion"]

# Exclude category
?asql=category!="Fashion"

# Cheap active auctions
?asql=status="Active" AND startingPrice<500

# Expensive art
?asql=category="Art" AND startingPrice>5000
```

### Bids

```bash
# User's bids
?asql=bidderId=1

# High value bids
?asql=amount>=1000

# Bids on product
?asql=productId=5

# User's high bids
?asql=bidderId=1 AND amount>=100

# Bid range
?asql=amount>=100 AND amount<=1000
```

---

## Syntax Rules

### ✅ Correct

```
category="Electronics"              String with quotes
startingPrice>=1000                 Number without quotes
category in ["Art", "Fashion"]      Array with quotes on strings
category="Art" AND price>100        Spaces around operators (optional)
productId=1 OR productId=2          Multiple conditions
```

### ❌ Incorrect

```
category=Electronics                Missing quotes on string
startingPrice>="1000"               Quotes on number
category in [Art, Fashion]          Missing quotes in array
category="Art" and price>100        Lowercase 'and' (should be AND)
category = "Art"price>100           Missing operator between conditions
```

---

## Common Patterns

### Price Filters
```bash
# Budget items
?asql=startingPrice<100

# Mid-range
?asql=startingPrice>=100 AND startingPrice<=1000

# Premium
?asql=startingPrice>1000
```

### Category Filters
```bash
# Single category
?asql=category="Electronics"

# Multiple categories
?asql=category in ["Electronics", "Fashion", "Art"]

# Exclude category
?asql=category!="Fashion"
```

### Status Filters
```bash
# Active auctions only
?asql=status="Active"

# Not expired
?asql=status!="Expired"
```

### Combined Filters
```bash
# Active Electronics under $500
?asql=status="Active" AND category="Electronics" AND startingPrice<500

# Art or Fashion over $1000
?asql=(category="Art" OR category="Fashion") AND startingPrice>1000
# Note: Parentheses not currently supported - use separate queries

# Short active auctions
?asql=status="Active" AND auctionDuration<60
```

---

## URL Encoding

When using ASQL in URLs, special characters must be encoded:

| Character | URL Encoded | Example |
|-----------|-------------|---------|
| Space | `%20` or `+` | `name="Vintage+Watch"` |
| `"` | `%22` | `category=%22Art%22` |
| `=` | `%3D` | (part of query, not encoded) |
| `&` | `%26` | (use AND instead) |

### Examples

```bash
# Browser/Postman (no encoding needed)
?asql=category="Art" AND startingPrice>1000

# cURL (quotes need escaping)
curl "http://localhost:6000/api/products?asql=category=\"Art\"+AND+startingPrice>1000"

# JavaScript fetch (automatic encoding)
fetch(`/api/products?asql=${encodeURIComponent('category="Art" AND startingPrice>1000')}`)
```

---

## Testing ASQL

### Using cURL

```bash
# Simple query
curl "http://localhost:6000/api/products?asql=productId=1"

# With pagination
curl "http://localhost:6000/api/products?asql=category=\"Electronics\"&pageNumber=1&pageSize=10"

# Complex query
curl "http://localhost:6000/api/products?asql=status=\"Active\"+AND+startingPrice<1000"
```

### Using Postman

1. Set method to `GET`
2. Enter URL: `http://localhost:6000/api/products`
3. Add Query Param:
   - Key: `asql`
   - Value: `category="Electronics" AND startingPrice>=1000`
4. Send request

### Using JavaScript

```javascript
// Simple
const query = 'category="Electronics"';
const response = await fetch(`/api/products?asql=${encodeURIComponent(query)}`);

// With pagination
const params = new URLSearchParams({
  asql: 'category="Electronics" AND startingPrice>=1000',
  pageNumber: 1,
  pageSize: 10
});
const response = await fetch(`/api/products?${params}`);
```

---

## Error Messages

### Common Errors

```json
// Unterminated string
{
  "message": "Invalid ASQL query",
  "error": "Unterminated string at position 12"
}

// Missing operator
{
  "message": "Invalid ASQL query", 
  "error": "Expected operator after field 'category'"
}

// Type mismatch
{
  "message": "Invalid ASQL query",
  "error": "Cannot convert value 'abc' to type Decimal for field 'startingPrice'"
}

// Unknown field
{
  "message": "Invalid ASQL query",
  "error": "Field 'unknownField' does not exist on Product"
}
```

---

## Tips & Tricks

### 1. Field Names are Case-Insensitive
```bash
?asql=ProductId=1          # Works
?asql=productid=1          # Works
?asql=PRODUCTID=1          # Works
```

### 2. String Values are Case-Sensitive
```bash
?asql=category="Electronics"    # Matches "Electronics"
?asql=category="electronics"    # Does NOT match "Electronics"
```

### 3. Whitespace is Flexible
```bash
?asql=productId=1              # Works
?asql=productId = 1            # Works
?asql=productId   =   1        # Works
```

### 4. Use OR for Multiple IDs
```bash
# Get products 1, 2, or 3
?asql=productId=1 OR productId=2 OR productId=3

# Or use IN operator
?asql=productId in [1, 2, 3]
```

### 5. Combine with Pagination
```bash
# First page of Electronics
?asql=category="Electronics"&pageNumber=1&pageSize=10

# Second page
?asql=category="Electronics"&pageNumber=2&pageSize=10
```

---

## Performance Tips

1. **Use Specific Fields**: `productId=1` is faster than `name="Product"`
2. **Avoid OR on Large Sets**: Use `in` operator instead
3. **Index Filtering**: Filters on indexed fields (productId, category) are faster
4. **Limit Page Size**: Smaller page sizes (10-20) perform better
5. **Cache Results**: Consider caching frequent queries client-side

---

## Limitations

- ❌ No nested expressions (no parentheses)
- ❌ No wildcards in strings (no `name like "%watch%"`)
- ❌ No sorting (use separate orderBy parameter)
- ❌ No aggregations (COUNT, SUM, etc.)
- ❌ No joins across entities
- ✅ Single-level AND/OR only

---

## FAQ

**Q: Can I use parentheses for grouping?**  
A: Not currently supported. Use separate queries or flat AND/OR.

**Q: How do I search for partial text?**  
A: Currently not supported. Exact matches only.

**Q: Can I sort results?**  
A: Not via ASQL. Results come in database order. Sorting may be added in future.

**Q: Is there a query length limit?**  
A: URL length limit (~2000 chars) applies. Keep queries reasonable.

**Q: Can I query related entities?**  
A: Only fields on the main entity. Can't query through relationships.

**Q: How do I debug my query?**  
A: Check browser Network tab for error details, or test with Postman.

---

## Cheat Sheet

```bash
# Operators
=   !=   <   <=   >   >=   in

# Logical
AND   OR

# Products
?asql=productId=1
?asql=category="Electronics"
?asql=startingPrice>=1000
?asql=status="Active"
?asql=category in ["Art", "Fashion"]
?asql=category="Art" AND startingPrice>1000

# Bids
?asql=bidderId=1
?asql=amount>=100
?asql=productId=5
?asql=bidderId=1 AND amount>=100

# With Pagination
?asql=category="Electronics"&pageNumber=1&pageSize=10
```

---

**Quick Start:**
1. Choose your field
2. Pick an operator
3. Add your value (strings in quotes)
4. Combine with AND/OR if needed
5. Test in browser or Postman

**Need Help?** Check `MILESTONE_3_DOCUMENTATION.md` for full details.

---

**Last Updated:** November 25, 2024  
**Version:** 1.0.0

