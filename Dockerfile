# 1. Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# copy csproj và restore trước (tối ưu cache)
COPY *.csproj ./
RUN dotnet restore

# copy toàn bộ source
COPY . ./
RUN dotnet publish -c Release -o out

# 2. Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# mở cổng API (ví dụ 80)
EXPOSE 80

ENTRYPOINT ["dotnet", "ELearning_ToanHocHay_Control.dll"]
