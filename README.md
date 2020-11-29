# how to run the backend
Download SQL Server 2019 Express
https://go.microsoft.com/fwlink/?linkid=866658

After the installation check that the `Server=...` part in the Connection String matches the one in the `\Webshop\appsettings.json`

Open Package Manager Console (Tools -> NuGet Package Manager -> Package Manager Console)
```
PM> Update-Database
PM> dotnet dev-certs https --trust
```

Run the project (not IIS Express), choose Webshop from the dropdown

admin
Adminpass1*

You can modify it in `\Webshop\appsettings.json`
=======

