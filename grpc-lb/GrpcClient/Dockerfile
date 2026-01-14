# 构建阶段
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 复制项目文件
COPY ["Shared/Shared.csproj", "Shared/"]
COPY ["GrpcClient/GrpcClient.csproj", "GrpcClient/"]

# 恢复依赖
RUN dotnet restore "GrpcClient/GrpcClient.csproj"

# 复制所有源代码
COPY Shared/ Shared/
COPY GrpcClient/ GrpcClient/

# 构建项目
WORKDIR "/src/GrpcClient"
RUN dotnet build "GrpcClient.csproj" -c Release -o /app/build

# 发布阶段
FROM build AS publish
RUN dotnet publish "GrpcClient.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 运行阶段
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# 设置入口点
ENTRYPOINT ["dotnet", "GrpcClient.dll"]
