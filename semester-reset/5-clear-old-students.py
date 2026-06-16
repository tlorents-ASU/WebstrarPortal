# Step 5 — Remove last semester's student->site mappings for sites 5-67 from DynamoDB.
# Run on LOCAL machine: python 5-clear-old-students.py
# Scans WebstrarUsers, deletes every item whose site is in 5..67.
# Does NOT touch instructor (1-3), demo/TA (199/200/201), or Janaka (99/100).

import boto3
from boto3.dynamodb.conditions import Attr

TABLE_NAME = "WebstrarUsers"
REGION = "us-west-2"
FIRST, LAST = 5, 67

table = boto3.resource("dynamodb", region_name=REGION).Table(TABLE_NAME)

# Scan for items in the site range (site is not a key, so scan + filter)
items = []
resp = table.scan(FilterExpression=Attr("site").between(FIRST, LAST))
items.extend(resp["Items"])
while "LastEvaluatedKey" in resp:
    resp = table.scan(
        FilterExpression=Attr("site").between(FIRST, LAST),
        ExclusiveStartKey=resp["LastEvaluatedKey"],
    )
    items.extend(resp["Items"])

print(f"Found {len(items)} student records mapped to sites {FIRST}-{LAST}.")
if not items:
    print("Nothing to delete.")
    raise SystemExit

with table.batch_writer() as batch:
    for it in items:
        batch.delete_item(Key={"asurite": it["asurite"], "site": it["site"]})

print(f"Deleted {len(items)} old records. Now load new roster with load-students.py.")
