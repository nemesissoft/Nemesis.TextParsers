using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebDemo.Services;

public class EnumSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Enum is { Count: > 0 } && context.Type is { IsEnum: true })
        {
            schema.Type = "string";
            //schema.Format = string.Empty;
            schema.Enum.Clear();
            Enum.GetNames(context.Type)
                .ToList()
                .ForEach(name => schema.Enum.Add(new OpenApiString(name)));

            var values = Enum.GetValues(context.Type).Cast<byte>().Zip(Enum.GetNames(context.Type))
                .Select(p => $"{p.Second}={p.First}");

            schema.Description = string.Join(", ", values);
        }
    }
}
