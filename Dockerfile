FROM mcr.microsoft.com/dotnet/aspnet:5.0
ENV TZ=America/Sao_Paulo
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone
RUN apt update
RUN DEBIAN_FRONTEND="noninteractive" 
RUN apt -y install tzdata
COPY bin/Release/net5.0/publish/ App/
WORKDIR /App
ENTRYPOINT ["dotnet", "api.stab.dll"]