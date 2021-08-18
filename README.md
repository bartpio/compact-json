# Compact JSON
Provides compact serialization and deserialization of IEnumerable<IDictionary<string, object>> record sets.
Useful when both sides are under your control, with both sides using this library. Not great for general interoperability.
Please see unit tests for example usage.

# Example Output
```json
{
  "keys": [
    "personid",
    "somemoney",
    "customercode"
  ],
  "types": [
    "System.Int32",
    "System.Decimal",
    "System.String"
  ],
  "rowcount": 10,
  "rows": [
    [
      "101",
      0,
      "customer 1000"
    ],
    [
      "111",
      1,
      100000.01,
      "customer 1001"
    ],
    [
      "111",
      2,
      200000.02,
      "customer 1002"
    ],
    [
      "111",
      3,
      300000.03,
      "customer 1003"
    ],
    [
      "111",
      4,
      400000.04,
      "customer 1004"
    ],
    [
      "111",
      5,
      500000.05,
      "customer 1005"
    ],
    [
      "111",
      6,
      600000.06,
      "customer 1006"
    ],
    [
      "111",
      7,
      700000.07,
      "customer 1007"
    ],
    [
      "111",
      8,
      800000.08,
      "customer 1008"
    ],
    [
      "111",
      9,
      900000.09,
      "customer 1009"
    ]
  ]
}
```
