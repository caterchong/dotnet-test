# 构建阶段 - 强制使用 linux/amd64 平台
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 设置环境变量强制使用 x64 架构的工具
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV DOTNET_RUNTIME_IDENTIFIER=linux-x64
ENV PROTOC_BIN_PATH=""

# 复制项目文件
COPY ["Shared/Shared.csproj", "Shared/"]
COPY ["GrpcServer/GrpcServer.csproj", "GrpcServer/"]

# 恢复依赖 - 使用 linux-x64 RID，并设置 MSBuild 属性强制使用 x64 protoc
RUN dotnet restore "GrpcServer/GrpcServer.csproj" -r linux-x64 /p:RuntimeIdentifier=linux-x64

# 复制所有源代码
COPY Shared/ Shared/
COPY GrpcServer/ GrpcServer/

# 构建项目 - 使用 linux-x64 运行时
# 由于使用了 --platform=linux/amd64，protoc 工具应该自动使用正确的架构
WORKDIR "/src/GrpcServer"
RUN dotnet build "GrpcServer.csproj" -c Release -o /app/build -r linux-x64 --self-contained false

# 发布阶段
FROM build AS publish
RUN dotnet publish "GrpcServer.csproj" -c Release -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

# 运行阶段 - 使用 linux/amd64 平台
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# 暴露端口
EXPOSE 50051

# 设置入口点
ENTRYPOINT ["dotnet", "GrpcServer.dll"]
