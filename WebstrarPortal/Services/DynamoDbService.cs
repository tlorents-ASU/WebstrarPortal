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

    /// <summary>
    /// Scans the entire table and returns a mapping of site number → list of ASURITEs.
    /// Used by instructor dashboard to show all student sites.
    /// </summary>
    public async Task<Dictionary<int, List<string>>> GetAllSiteAssignmentsAsync()
    {
        var result = new Dictionary<int, List<string>>();
        Dictionary<string, AttributeValue>? lastKey = null;

        do
        {
            var request = new ScanRequest
            {
                TableName = _tableName,
                ProjectionExpression = "asurite, site"
            };

            if (lastKey != null)
                request.ExclusiveStartKey = lastKey;

            var response = await _ddb.ScanAsync(request);

            foreach (var item in response.Items)
            {
                if (!item.TryGetValue("site", out var siteAttr) || string.IsNullOrWhiteSpace(siteAttr.N))
                    continue;
                if (!item.TryGetValue("asurite", out var asuriteAttr) || string.IsNullOrWhiteSpace(asuriteAttr.S))
                    continue;

                var site = int.Parse(siteAttr.N);
                var asurite = asuriteAttr.S;

                if (!result.ContainsKey(site))
                    result[site] = new List<string>();

                result[site].Add(asurite);
            }

            lastKey = response.LastEvaluatedKey?.Count > 0 ? response.LastEvaluatedKey : null;
        } while (lastKey != null);

        return result;
    }
}
