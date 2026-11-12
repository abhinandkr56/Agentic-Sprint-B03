using Microsoft.Extensions.VectorData;

namespace EmbeddingsDemo;

public class Faq
{
    [VectorStoreKey]public int Id { get; set; }
    [VectorStoreData]public string Text { get; set; }
    
    [VectorStoreVector(dimensions:3072, 
        DistanceFunction = DistanceFunction.CosineSimilarity)]public string Embedding => Text;
}

// cosineDistance = 1- cosineSimilarity;;