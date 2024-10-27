CREATE DATABASE STEMKITshop;
USE STEMKITshop;

-- Start a transaction
BEGIN TRANSACTION;

BEGIN TRY
    -- Users Table
    CREATE TABLE Users (
      UserID INT IDENTITY(1,1) PRIMARY KEY,
      FullName NVARCHAR(255),
      Username NVARCHAR(50) NOT NULL,
      Password NVARCHAR(255),
      Email NVARCHAR(255) NOT NULL,
      Phone NVARCHAR(255),
      Address NVARCHAR(255),
      Status BIT NOT NULL,
      IsExternal BIT NOT NULL DEFAULT 0,
      ExternalProvider NVARCHAR(50)
    );

    -- Roles Table
    CREATE TABLE Roles (
      RoleID INT IDENTITY(1,1) PRIMARY KEY,
      RoleName NCHAR(20) NOT NULL
    );

    -- Insert roles into Roles table
    INSERT INTO Roles (RoleName) VALUES 
    ('Customer'), 
    ('Staff'), 
    ('Manager');

    -- UserRoles Table
    CREATE TABLE UserRoles (
      UserRoleID INT IDENTITY(1,1) PRIMARY KEY,
      UserID INT NOT NULL,
      RoleID INT NOT NULL,
      FOREIGN KEY (UserID) REFERENCES Users(UserID),
      FOREIGN KEY (RoleID) REFERENCES Roles(RoleID)
    );

    -- Permissions Table
    CREATE TABLE Permissions (
      PermissionID INT IDENTITY(1,1) PRIMARY KEY,
      PermissionName VARCHAR(100) NOT NULL,
      Description NTEXT
    );

    -- UserPermissions Table
    CREATE TABLE UserPermissions (
      UserPermissionID INT IDENTITY(1,1) PRIMARY KEY,
      UserID INT NOT NULL,
      PermissionID INT NOT NULL,
      AssignedBy INT NOT NULL,
      FOREIGN KEY (UserID) REFERENCES Users(UserID),
      FOREIGN KEY (PermissionID) REFERENCES Permissions(PermissionID),
      FOREIGN KEY (AssignedBy) REFERENCES Users(UserID)
    );

    -- RefreshToken Table
    CREATE TABLE RefreshToken (
      Id INT IDENTITY(1,1) PRIMARY KEY,
      UserID INT NOT NULL,
      ExpirationTime DATETIME NOT NULL,
      Revoked DATETIME,
      RevokedByIp NVARCHAR(45),
      ReplacedByToken NVARCHAR(255),
      Token NVARCHAR(255) NOT NULL DEFAULT '',
      Created DATETIME NOT NULL DEFAULT GETUTCDATE(),
      CreatedByIp NVARCHAR(45) NOT NULL DEFAULT '',
      FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE
    );

    -- Subcategories Table
    CREATE TABLE Subcategories (
      SubcategoryID INT IDENTITY(1,1) PRIMARY KEY,
      SubcategoryName VARCHAR(100) NOT NULL,
      Description VARCHAR(MAX)
    );

    -- Labs Table
    CREATE TABLE Labs (
      LabID INT IDENTITY(1,1) PRIMARY KEY,
      LabName NVARCHAR(255),
      Description NVARCHAR(MAX),
      LabFileURL NVARCHAR(255)
    );

    -- Products Table
    CREATE TABLE Products (
      ProductID INT IDENTITY(1,1) PRIMARY KEY,
      ProductName NVARCHAR(255) NOT NULL,
      Description NVARCHAR(MAX) NOT NULL,
      Price DECIMAL(10, 2) NOT NULL,
      StockQuantity INT NOT NULL,
      Ages NCHAR(10),
      SupportInstances INT NOT NULL,
      LabID INT NOT NULL,
      SubcategoryID INT NOT NULL,
      FOREIGN KEY (LabID) REFERENCES Labs(LabID),
      FOREIGN KEY (SubcategoryID) REFERENCES Subcategories(SubcategoryID)
    );

    -- Carts Table
    CREATE TABLE Carts (
      CartID INT IDENTITY(1,1) PRIMARY KEY,
      UserID INT NOT NULL,
      CreatedDate DATE NOT NULL,
      Status NVARCHAR(255) NOT NULL,
      FOREIGN KEY (UserID) REFERENCES Users(UserID)
    );

    -- CartItems Table
    CREATE TABLE CartItems (
      CartItemID INT IDENTITY(1,1) PRIMARY KEY,
      CartID INT NOT NULL,
      ProductID INT NOT NULL,
      Quantity INT NOT NULL,
      Price DECIMAL(10, 2) NOT NULL,
      FOREIGN KEY (CartID) REFERENCES Carts(CartID),
      FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
    );

    -- Orders Table
    CREATE TABLE Orders (
      OrderID INT IDENTITY(1,1) PRIMARY KEY,
      UserID INT NOT NULL,
      OrderDate DATE,
      TotalAmount DECIMAL(10, 2),
      FOREIGN KEY (UserID) REFERENCES Users(UserID)
    );

    -- OrderDetails Table
    CREATE TABLE OrderDetails (
      OrderDetailID INT IDENTITY(1,1) PRIMARY KEY,
      OrderID INT NOT NULL,
      ProductID INT NOT NULL,
      ProductDescription NVARCHAR(MAX),
      Quantity INT,
      Price DECIMAL(10, 2),
      FOREIGN KEY (OrderID) REFERENCES Orders(OrderID),
      FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
    );

    -- Deliveries Table
    CREATE TABLE Deliveries (
      DeliveryID INT IDENTITY(1,1) PRIMARY KEY,
      OrderID INT,
      DeliveryStatus NVARCHAR(255),
      DeliveryDate DATE,
      FOREIGN KEY (OrderID) REFERENCES Orders(OrderID)
    );

    -- Invoices Table
    CREATE TABLE Invoices (
      InvoiceID INT IDENTITY(1,1) PRIMARY KEY,
      OrderID INT,
      InvoiceDate DATE,
      TotalAmount DECIMAL(10, 2),
      InvoiceType NVARCHAR(255),
      Note NVARCHAR(MAX),
      FOREIGN KEY (OrderID) REFERENCES Orders(OrderID)
    );

    -- InvoiceDetails Table
    CREATE TABLE InvoiceDetails (
      InvoiceDetailID INT IDENTITY(1,1) PRIMARY KEY,
      InvoiceID INT,
      ProductName NVARCHAR(255),
      Quantity INT,
      Price DECIMAL(10, 2),
      FOREIGN KEY (InvoiceID) REFERENCES Invoices(InvoiceID)
    );

    -- SupportRequest Table
    CREATE TABLE SupportRequest (
      SupportID INT IDENTITY(1,1) PRIMARY KEY,
      SupportInstance INT NOT NULL,
      UserID INT NOT NULL,
      OrderDetailID INT NOT NULL,
      FOREIGN KEY (UserID) REFERENCES Users(UserID),
      FOREIGN KEY (OrderDetailID) REFERENCES OrderDetails(OrderDetailID)
    );

    -- If everything succeeds, commit the transaction
    COMMIT TRANSACTION;
    PRINT 'Transaction committed successfully.';

END TRY
BEGIN CATCH
    -- If any error occurs, roll back the transaction
    ROLLBACK TRANSACTION;
    PRINT 'Transaction rolled back due to an error: ' + ERROR_MESSAGE();
END CATCH;
