BEGIN TRY
    BEGIN TRANSACTION;

    -- ============================================
    -- Migration: Add VIEW_USER and VIEW_USERS capabilities
    -- Assign to ADMIN and EMPLOYEE roles
    -- ============================================

    -- ============================================
    -- Insert new Capabilities (idempotent)
    -- ============================================
    INSERT INTO [dbo].[Capabilities] ([Name], [Description])
    SELECT [Name], [Description]
    FROM (VALUES    -- Το VALUES (...), (...) είναι ο row constructor που παράγει ένα inline σύνολο από rows.
        ('VIEW_USER',  'View user'),
        ('VIEW_USERS', 'View user list and details')
    ) AS NewCaps([Name], [Description])     --  δίνει alias στο derived table και ονόματα στις στήλες του.
    WHERE NOT EXISTS (  -- ελέγχει αν το subquery επιστρέφει έστω και ένα row
        SELECT 1        -- Το SELECT 1 στο EXISTS δεν επιστρέφει ποτέ τιμή στο εξωτερικό query. Το 1 είναι απλώς σύμβαση 
        FROM [dbo].[Capabilities] c
        WHERE c.[Name] = NewCaps.[Name]
    );


    -- ============================================
    -- Assign to ADMIN (idempotent)
    -- ============================================
    INSERT INTO [dbo].[RolesCapabilities] ([RolesId], [CapabilitiesId])
    SELECT r.[Id], c.[Id]
    FROM [dbo].[Roles] r
    CROSS JOIN [dbo].[Capabilities] c
    WHERE r.[Name] = 'ADMIN'
      AND c.[Name] IN ('VIEW_USER', 'VIEW_USERS')
      AND NOT EXISTS (
          SELECT 1
          FROM [dbo].[RolesCapabilities] rc
          WHERE rc.[RolesId] = r.[Id] AND rc.[CapabilitiesId] = c.[Id]
      );


    -- ============================================
    -- Assign to EMPLOYEE (idempotent)
    -- ============================================
    INSERT INTO [dbo].[RolesCapabilities] ([RolesId], [CapabilitiesId])
    SELECT r.[Id], c.[Id]
    FROM [dbo].[Roles] r
    CROSS JOIN [dbo].[Capabilities] c
    WHERE r.[Name] = 'EMPLOYEE'
      AND c.[Name] IN ('VIEW_USER', 'VIEW_USERS')
      AND NOT EXISTS (
          SELECT 1
          FROM [dbo].[RolesCapabilities] rc
          WHERE rc.[RolesId] = r.[Id] AND rc.[CapabilitiesId] = c.[Id]
      );


    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    THROW;
END CATCH;