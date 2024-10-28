using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcService;

namespace gRPC_Server.Services
{
    public class ProductService:Product.ProductBase
    {
        public override Task<Products> GetAll(Empty request, ServerCallContext context)
        {
            Console.WriteLine($"Product service GetAll method started executing...");
            var products = new List<ProductModel>()
            {
                new ProductModel(){
                    Name = "Unary gRPC communication mechanism 1",
                    Id = 1
                },
                new ProductModel(){
                    Name = "Unary gRPC communication mechanism 2",
                    Id = 2
                }
            };
            var ProductList= new Products();
            ProductList.ProductsList.AddRange(products);
            return Task.FromResult(ProductList);
        }

        public override async Task ServerStreamingDemo(Empty request, IServerStreamWriter<ProductModel> responseStream, ServerCallContext context)
        {
            for (var i = 1; i <= 10; i++)
            {
                await responseStream.WriteAsync(new ProductModel() { Name = $"Product Stream{i}", Id = i });
                await Task.Delay(10000);
            }
        }
        public override async Task<Products> ClientStreamingDemo(IAsyncStreamReader<ProductModel> requestStream, ServerCallContext context)
        {
            var products = new Products();
            while (await requestStream.MoveNext()) {
                products.ProductsList.Add(requestStream.Current);
            }
            return products;
        }

        public override async Task BidirectionalStreamingDemo(IAsyncStreamReader<ProductModel> requestStream, IServerStreamWriter<ProductModel> responseStream, ServerCallContext context)
        {
            var responseStreamTask = Task.Run(async () =>
            {
                for (var i = 1; i <= 10; i++)
                {
                    await responseStream.WriteAsync(new ProductModel() { Name = $"Product Stream{i}", Id = i });
                    await Task.Delay(1000);
                }
            });

            var requestStreamTask = Task.Run(async () =>
            {
                var products = new Products();
                while (await requestStream.MoveNext())
                {
                    products.ProductsList.Add(requestStream.Current);
                }
            });
            await Task.WhenAll(responseStreamTask, responseStreamTask);
        }
    }
}
