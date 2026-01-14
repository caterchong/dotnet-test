# 构建阶段
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 复制项目文件
COPY ["Shared/Shared.csproj", "Shared/"]
COPY ["GrpcServer/GrpcServer.csproj", "GrpcServer/"]

# 恢复依赖
RUN dotnet restore "GrpcServer/GrpcServer.csproj"

# 复制所有源代码
COPY Shared/ Shared/
COPY GrpcServer/ GrpcServer/

# 构建项目
WORKDIR "/src/GrpcServer"
RUN dotnet build "GrpcServer.csproj" -c Release -o /app/build

# 发布阶段
FROM build AS publish
RUN dotnet publish "GrpcServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 运行阶段
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# 暴露端口
EXPOSE 50051

# 设置入口点
ENTRYPOINT ["dotnet", "GrpcServer.dll"]
