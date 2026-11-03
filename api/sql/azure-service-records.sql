IF OBJECT_ID(N'dbo.service_records', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.service_records (
        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        service_name VARCHAR(200) NOT NULL,
        environment VARCHAR(100) NOT NULL,
        status VARCHAR(100) NOT NULL,
        created_at DATETIMEOFFSET NOT NULL
            CONSTRAINT DF_service_records_created_at DEFAULT SYSDATETIMEOFFSET()
    );
END;
