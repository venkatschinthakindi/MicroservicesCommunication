using Grpc.Net.Client;
using GrpcService;
using Microsoft.AspNetCore.Mvc;

namespace gRPC_Client.Controllers
{
    [ApiController]
    [Route("Product")]
    public class ProductController : ControllerBase
    {
        private readonly ILogger<ProductController> _logger;

        public ProductController(ILogger<ProductController> logger)
        {
            _logger = logger;
        }

        [HttpGet("GetAll")]
        public async Task<IEnumerable<ProductModel>> GetAll()
        {
            var channel = GrpcChannel.ForAddress("https://localhost:7165/");
            var client = new Product.ProductClient(channel);
            var resposne = await client.GetAllAsync(new Google.Protobuf.WellKnownTypes.Empty());
            
            await channel.ShutdownAsync();
            return resposne.ProductsList;
        }

        [HttpGet("ServerStreamingDemo")]
        public async Task<IEnumerable<ProductModel>> ServerStreamingDemo()
        {
            var products = new List<ProductModel>();
            var channel = GrpcChannel.ForAddress("https://localhost:7165/");
            var client = new Product.ProductClient(channel);
            var resposne =  client.ServerStreamingDemo(new Google.Protobuf.WellKnownTypes.Empty());
            while (await resposne.ResponseStream.MoveNext(CancellationToken.None))
            {
                var productModel = resposne.ResponseStream.Current;
                products.Add(productModel);
            }
            await channel.ShutdownAsync();
            return products;
        }

        [HttpGet("ClientStreamingDemo")]
        public async Task<List<ProductModel>> ClientStreamingDemo()
        {
            var products = new List<ProductModel>();
            var channel = GrpcChannel.ForAddress("https://localhost:7165/");
            var client = new Product.ProductClient(channel);
            
            using (var requestStream = client.ClientStreamingDemo())
            {
                for (var i = 1; i <= 10; i++)
                {
                    await requestStream.RequestStream.WriteAsync(new ProductModel()
                    {
                        Id= i,
                        Name=$"Client streaming request-{i}"
                    });
                }
                await requestStream.RequestStream.CompleteAsync();
                var resProducts = await requestStream.ResponseAsync;
                return resProducts.ProductsList.ToList();
            }
        }

        [HttpGet("BidirectionalStreamingDemo")]
        public async Task<List<ProductModel>> BidirectionalStreamingDemo()
        {
            var products = new List<ProductModel>();
            var channel = GrpcChannel.ForAddress("https://localhost:7165/");
            var client = new Product.ProductClient(channel);

            var resposeProducts = new List<ProductModel>();
            using(var  requestStream = client.BidirectionalStreamingDemo())
            {
                var responseStreamTask = Task.Run(async () =>
                {
                    while (await requestStream.ResponseStream.MoveNext(CancellationToken.None))
                    {
                        resposeProducts.Add(requestStream.ResponseStream.Current);
                    }
                });

                var requestStreamTask = Task.Run(async () =>
                {
                    for (var i = 1; i <= 10; i++)
                    {
                        await requestStream.RequestStream.WriteAsync(new ProductModel()
                        {
                            Id = i,
                            Name = $"Bidirectional request stream - {i}"
                        });
                    }
                });

                await Task.WhenAll(responseStreamTask, requestStreamTask);               
                await requestStream.RequestStream.CompleteAsync();
            }

            return resposeProducts;
        }
    }
}
