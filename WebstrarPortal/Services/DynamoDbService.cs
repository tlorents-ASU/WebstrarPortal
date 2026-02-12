using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace WebstrarPortal.Services;

public class DynamoDbService
{
    private readonly IAmazonDynamoDB _ddb;
    private readonly string _tableName;

    public DynamoDbService(IAmazonDynamoDB ddb, string tableName)
    {
        _ddb = ddb;
        _tableName = tableName;
    }

    public async Task<int?> GetSiteForAsuriteAsync(string asurite)
    {
        // Table key is (asurite HASH, site RANGE)
        // So we Query on asurite and take the first result.
        var response = await _ddb.QueryAsync(new QueryRequest
        {
            TableName = _tableName,
            KeyConditionExpression = "asurite = :a",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":a"] = new AttributeValue { S = asurite }
            },
            ProjectionExpression = "site",
            Limit = 1,
            ConsistentRead = true
        });

        var item = response.Items?.FirstOrDefault();
        if (item == null) return null;

        if (!item.TryGetValue("site", out var siteAttr)) return null;
        if (string.IsNullOrWhiteSpace(siteAttr.N)) return null;

        return int.Parse(siteAttr.N);
    }
}
