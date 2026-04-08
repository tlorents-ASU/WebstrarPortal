import csv
import boto3
import sys

TABLE_NAME = "WebstrarUsers"
REGION = "us-west-2"

if len(sys.argv) < 2:
    print("Usage: python load-students.py <path-to-csv>")
    sys.exit(1)

csv_path = sys.argv[1]
dynamodb = boto3.resource("dynamodb", region_name=REGION)
table = dynamodb.Table(TABLE_NAME)

count = 0
skipped = 0

with open(csv_path, newline="", encoding="utf-8-sig") as f:
    reader = csv.DictReader(f)
    with table.batch_writer() as batch:
        for row in reader:
            asurite = row["asurite"].strip().lower()
            site = int(row["site"].strip())
            if not asurite:
                skipped += 1
                continue
            batch.put_item(Item={"asurite": asurite, "site": site})
            count += 1

print(f"Done. Loaded {count} students, skipped {skipped} rows.")
