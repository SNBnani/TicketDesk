namespace TicketDesk.Domain.Legacy.Migrations
{
	using System;
	using System.Linq;

	using System.Data.Entity.Migrations;

	public partial class Initial : DbMigration
	{
		public override void Up()
		{
			DropTable("ELMAH_Error");
			Sql("DROP PROCEDURE [dbo].[ELMAH_GetErrorsXml]");
			Sql("DROP PROCEDURE [dbo].[ELMAH_GetErrorXml]");
			Sql("DROP PROCEDURE [dbo].[ELMAH_LogError]");

			DropForeignKey("TicketEventNotifications", "FK_TicketEventNotifications_TicketComments");

			AddForeignKey("TicketEventNotifications", new[] { "TicketId", "CommentId" }, "TicketComments", cascadeDelete: true);

			CreateIndex("TicketTags", "TicketId", false, "IX_TicketId");
			CreateIndex("TicketEventNotifications", new[] { "TicketId", "CommentId" }, false, "IX_TicketId_CommentId");
			CreateIndex("TicketComments", "TicketId", false, "IX_TicketId");
			CreateIndex("TicketAttachments", "TicketId", false, "IX_TicketId");
            CreateTable(
               "dbo.AspNetRoles",
               c => new
               {
                   Id = c.String(nullable: false, maxLength: 128),
                   Name = c.String(nullable: false, maxLength: 256),
               })
               .PrimaryKey(t => t.Id)
               .Index(t => t.Name, unique: true, name: "RoleNameIndex");

            CreateTable(
                 "dbo.AspNetUsers",
                 c => new
                 {
                     Id = c.String(nullable: false, maxLength: 128),
                     Email = c.String(nullable: false, maxLength: 256),
                     EmailConfirmed = c.Boolean(nullable: false),
                     PasswordHash = c.String(),
                     SecurityStamp = c.String(),
                     PhoneNumber = c.String(),
                     PhoneNumberConfirmed = c.Boolean(nullable: false),
                     TwoFactorEnabled = c.Boolean(nullable: false),
                     LockoutEndDateUtc = c.DateTime(),
                     LockoutEnabled = c.Boolean(nullable: false),
                     AccessFailedCount = c.Int(nullable: false),
                     UserName = c.String(nullable: false, maxLength: 256),
                 })
                 .PrimaryKey(t => t.Id)
                 .Index(t => t.UserName, unique: true, name: "UserNameIndex");

            CreateTable(
                  "dbo.AspNetUserClaims",
                  c => new
                  {
                      Id = c.Int(nullable: false, identity: true),
                      UserId = c.String(nullable: false, maxLength: 128),
                      ClaimType = c.String(),
                      ClaimValue = c.String(),
                  })
                  .PrimaryKey(t => t.Id)
                  .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                  .Index(t => t.UserId);

            CreateTable(
                "dbo.AspNetUserLogins",
                c => new
                {
                    LoginProvider = c.String(nullable: false, maxLength: 128),
                    ProviderKey = c.String(nullable: false, maxLength: 128),
                    UserId = c.String(nullable: false, maxLength: 128),
                })
                .PrimaryKey(t => new { t.LoginProvider, t.ProviderKey, t.UserId })
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);

            CreateTable(
                "dbo.AspNetUserRoles",
                c => new
                {
                    UserId = c.String(nullable: false, maxLength: 128),
                    RoleId = c.String(nullable: false, maxLength: 128),
                })
                .PrimaryKey(t => new { t.UserId, t.RoleId })
                .ForeignKey("dbo.AspNetRoles", t => t.RoleId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId)
                .Index(t => t.RoleId);
			
			RenameColumn("dbo.Tickets", "Type", "TicketType");

			Sql(@"
				ALTER TABLE [dbo].[Tickets] DROP CONSTRAINT [DF_Tickets_CreatedDate];

				ALTER TABLE [dbo].[Tickets] ALTER COLUMN [CreatedDate] DATETIMEOFFSET (7) NOT NULL;
				ALTER TABLE [dbo].[Tickets] ALTER COLUMN [CurrentStatusDate] DATETIMEOFFSET (7) NOT NULL;
				ALTER TABLE [dbo].[Tickets] ALTER COLUMN [LastUpdateDate] DATETIMEOFFSET (7) NOT NULL;

				ALTER TABLE [dbo].[Tickets] ADD CONSTRAINT [DF_Tickets_CreatedDate] DEFAULT (sysdatetimeoffset()) FOR [CreatedDate];

				ALTER TABLE [dbo].[TicketComments] DROP CONSTRAINT [DF_TicketComments_CommentDate];

				ALTER TABLE [dbo].[TicketComments] ALTER COLUMN [CommentedDate] DATETIMEOFFSET (7) NOT NULL;

				ALTER TABLE [dbo].[TicketComments] ADD CONSTRAINT [DF_TicketComments_CommentDate] DEFAULT (sysdatetimeoffset()) FOR [CommentedDate];
				ALTER TABLE [dbo].[TicketAttachments] ALTER COLUMN [UploadedDate] DATETIMEOFFSET (7) NOT NULL;

				ALTER TABLE [dbo].[TicketEventNotifications] ALTER COLUMN [CreatedDate] DATETIMEOFFSET (7) NOT NULL;
				ALTER TABLE [dbo].[TicketEventNotifications] ALTER COLUMN [LastDeliveryAttemptDate] DATETIMEOFFSET (7) NULL;
				ALTER TABLE [dbo].[TicketEventNotifications] ALTER COLUMN [NextDeliveryAttemptDate] DATETIMEOFFSET (7) NULL;

				ALTER TABLE [dbo].[AdCachedUserProperties] ALTER COLUMN [LastRefreshed] DATETIMEOFFSET (7) NULL;
			");

            AddColumn("dbo.Tickets", "TicketStatus", c => c.Int(nullable: false));
            Sql("Update dbo.Tickets set TicketStatus = 0 where CurrentStatus = 'Active'");
            Sql("Update dbo.Tickets set TicketStatus = 1 where CurrentStatus = 'More Info'");
            Sql("Update dbo.Tickets set TicketStatus = 2 where CurrentStatus = 'Resolved'");
            Sql("Update dbo.Tickets set TicketStatus = 3 where CurrentStatus = 'Closed'");

            DropColumn("dbo.Tickets", "CurrentStatus");

			Sql(@"UPDATE [dbo].[Settings] SET [SettingValue] = '3.0.0.001' WHERE [SettingName] = 'Version'");
		}

		public override void Down()
		{
            DropForeignKey("dbo.AspNetUserClaims", "User_Id", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserRoles", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserRoles", "RoleId", "dbo.AspNetRoles");
            DropForeignKey("dbo.AspNetUserLogins", "UserId", "dbo.AspNetUsers");

            DropForeignKey("dbo.AspNetUserRoles", "RoleId", "dbo.AspNetRoles");
            DropIndex("dbo.AspNetUserLogins", new[] { "UserId" });
            DropIndex("dbo.AspNetUserClaims", new[] { "UserId" });
            DropIndex("dbo.AspNetUsers", "UserNameIndex");
            DropIndex("dbo.AspNetUserRoles", new[] { "RoleId" });
            DropIndex("dbo.AspNetUserRoles", new[] { "UserId" });
            DropIndex("dbo.AspNetRoles", "RoleNameIndex");
            DropTable("dbo.AspNetUserLogins");
            DropTable("dbo.AspNetUserClaims");
            DropTable("dbo.AspNetUsers");
            DropTable("dbo.AspNetUserRoles");
            DropTable("dbo.AspNetRoles");

			DropIndex("TicketTags", "IX_TicketId");
			DropIndex("TicketEventNotifications", "IX_TicketId_CommentId");
			DropIndex("TicketComments", "IX_TicketId");
			DropIndex("TicketAttachments", "IX_TicketId");
			
            DropTable("dbo.AspNetRoles");
			DropForeignKey("TicketEventNotifications", new[] { "TicketId", "CommentId" }, "TicketComments");

			AddForeignKey("TicketEventNotifications", new[] { "TicketId", "CommentId" }, "TicketComments", principalColumns: new[] { "TicketId", "CommentId" }, cascadeDelete: false, name: "FK_TicketEventNotifications_TicketComments");

			Sql(@"
				/****** Object:  Table [dbo].[ELMAH_Error]    Script Date: 12/05/2010 18:31:00 ******/
				SET ANSI_NULLS ON

				SET QUOTED_IDENTIFIER ON

				IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ELMAH_Error]') AND type in (N'U'))
				BEGIN
				CREATE TABLE [dbo].[ELMAH_Error](
					[ErrorId] [uniqueidentifier] NOT NULL CONSTRAINT [DF_ELMAH_Error_ErrorId]  DEFAULT (newid()),
					[Application] [nvarchar](60) NOT NULL,
					[Host] [nvarchar](50) NOT NULL,
					[Type] [nvarchar](100) NOT NULL,
					[Source] [nvarchar](60) NOT NULL,
					[Message] [nvarchar](500) NOT NULL,
					[User] [nvarchar](50) NOT NULL,
					[StatusCode] [int] NOT NULL,
					[TimeUtc] [datetime] NOT NULL,
					[Sequence] [int] IDENTITY(1,1) NOT NULL,
					[AllXml] [ntext] NOT NULL,
				 CONSTRAINT [PK_ELMAH_Error] PRIMARY KEY NONCLUSTERED 
				(
					[ErrorId] ASC
				)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
				) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
				END

				/****** Object:  StoredProcedure [dbo].[ELMAH_LogError]    Script Date: 12/05/2010 18:31:01 ******/
				SET ANSI_NULLS ON

				SET QUOTED_IDENTIFIER ON

				IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ELMAH_LogError]') AND type in (N'P', N'PC'))
				BEGIN
				EXEC dbo.sp_executesql @statement = N'
				CREATE PROCEDURE [dbo].[ELMAH_LogError]
				(
					@ErrorId UNIQUEIDENTIFIER,
					@Application NVARCHAR(60),
					@Host NVARCHAR(30),
					@Type NVARCHAR(100),
					@Source NVARCHAR(60),
					@Message NVARCHAR(500),
					@User NVARCHAR(50),
					@AllXml NTEXT,
					@StatusCode INT,
					@TimeUtc DATETIME
				)
				AS

					SET NOCOUNT ON

					INSERT
					INTO
						[ELMAH_Error]
						(
							[ErrorId],
							[Application],
							[Host],
							[Type],
							[Source],
							[Message],
							[User],
							[AllXml],
							[StatusCode],
							[TimeUtc]
						)
					VALUES
						(
							@ErrorId,
							@Application,
							@Host,
							@Type,
							@Source,
							@Message,
							@User,
							@AllXml,
							@StatusCode,
							@TimeUtc
						)

				' 
				END

				/****** Object:  StoredProcedure [dbo].[ELMAH_GetErrorXml]    Script Date: 12/05/2010 18:31:01 ******/
				SET ANSI_NULLS ON

				SET QUOTED_IDENTIFIER ON

				IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ELMAH_GetErrorXml]') AND type in (N'P', N'PC'))
				BEGIN
				EXEC dbo.sp_executesql @statement = N'
				CREATE PROCEDURE [dbo].[ELMAH_GetErrorXml]
				(
					@Application NVARCHAR(60),
					@ErrorId UNIQUEIDENTIFIER
				)
				AS

					SET NOCOUNT ON

					SELECT 
						[AllXml]
					FROM 
						[ELMAH_Error]
					WHERE
						[ErrorId] = @ErrorId
					AND
						[Application] = @Application

				' 
				END

				/****** Object:  StoredProcedure [dbo].[ELMAH_GetErrorsXml]    Script Date: 12/05/2010 18:31:01 ******/
				SET ANSI_NULLS ON

				SET QUOTED_IDENTIFIER ON

				IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ELMAH_GetErrorsXml]') AND type in (N'P', N'PC'))
				BEGIN
				EXEC dbo.sp_executesql @statement = N'
				CREATE PROCEDURE [dbo].[ELMAH_GetErrorsXml]
				(
					@Application NVARCHAR(60),
					@PageIndex INT = 0,
					@PageSize INT = 15,
					@TotalCount INT OUTPUT
				)
				AS 

					SET NOCOUNT ON

					DECLARE @FirstTimeUTC DATETIME
					DECLARE @FirstSequence INT
					DECLARE @StartRow INT
					DECLARE @StartRowIndex INT

					SELECT 
						@TotalCount = COUNT(1) 
					FROM 
						[ELMAH_Error]
					WHERE 
						[Application] = @Application

					-- Get the ID of the first error for the requested page

					SET @StartRowIndex = @PageIndex * @PageSize + 1

					IF @StartRowIndex <= @TotalCount
					BEGIN

						SET ROWCOUNT @StartRowIndex

						SELECT  
							@FirstTimeUTC = [TimeUtc],
							@FirstSequence = [Sequence]
						FROM 
							[ELMAH_Error]
						WHERE   
							[Application] = @Application
						ORDER BY 
							[TimeUtc] DESC, 
							[Sequence] DESC

					END
					ELSE
					BEGIN

						SET @PageSize = 0

					END

					-- Now set the row count to the requested page size and get
					-- all records below it for the pertaining application.

					SET ROWCOUNT @PageSize

					SELECT 
						errorId     = [ErrorId], 
						application = [Application],
						host        = [Host], 
						type        = [Type],
						source      = [Source],
						message     = [Message],
						[user]      = [User],
						statusCode  = [StatusCode], 
						time        = CONVERT(VARCHAR(50), [TimeUtc], 126) + ''Z''
					FROM 
						[ELMAH_Error] error
					WHERE
						[Application] = @Application
					AND
						[TimeUtc] <= @FirstTimeUTC
					AND 
						[Sequence] <= @FirstSequence
					ORDER BY
						[TimeUtc] DESC, 
						[Sequence] DESC
					FOR
						XML AUTO

				' 
				END

			");
			RenameColumn("dbo.Tickets", "TicketType", "Type");

			Sql(@"
				ALTER TABLE [dbo].[Tickets] DROP CONSTRAINT [DF_Tickets_CreatedDate];

				ALTER TABLE [dbo].[Tickets] ALTER COLUMN [CreatedDate] DATETIME NOT NULL;
				ALTER TABLE [dbo].[Tickets] ALTER COLUMN [CurrentStatusDate] DATETIME NOT NULL;
				ALTER TABLE [dbo].[Tickets] ALTER COLUMN [LastUpdateDate] DATETIME NOT NULL;

				ALTER TABLE [dbo].[Tickets] ADD CONSTRAINT [DF_Tickets_CreatedDate] DEFAULT (getdate()) FOR [CreatedDate];

				ALTER TABLE [dbo].[TicketComments] DROP CONSTRAINT [DF_TicketComments_CommentDate];

				ALTER TABLE [dbo].[TicketComments] ALTER COLUMN [CommentedDate] DATETIME NOT NULL;

				ALTER TABLE [dbo].[TicketComments] ADD CONSTRAINT [DF_TicketComments_CommentDate] DEFAULT (getdate()) FOR [CommentedDate];
				ALTER TABLE [dbo].[TicketAttachments] ALTER COLUMN [UploadedDate] DATETIME NOT NULL;

				ALTER TABLE [dbo].[TicketEventNotifications] ALTER COLUMN [CreatedDate] DATETIME NOT NULL;
				ALTER TABLE [dbo].[TicketEventNotifications] ALTER COLUMN [LastDeliveryAttemptDate] DATETIME NULL;
				ALTER TABLE [dbo].[TicketEventNotifications] ALTER COLUMN [NextDeliveryAttemptDate] DATETIME NULL;

				ALTER TABLE [dbo].[AdCachedUserProperties] ALTER COLUMN [LastRefreshed] DATETIME NULL;
			");

            AddColumn("dbo.Tickets", "CurrentStatus", c => c.String(nullable: false, maxLength: 50));

            Sql("Update dbo.Tickets set CurrentStatus = 'Active' where TicketStatus = 0 ");
            Sql("Update dbo.Tickets set CurrentStatus = 'More Info' where TicketStatus = 1 ");
            Sql("Update dbo.Tickets set CurrentStatus = 'Resolved' where TicketStatus = 2 ");
            Sql("Update dbo.Tickets set CurrentStatus = 'Closed' where TicketStatus = 3 ");

            DropColumn("dbo.Tickets", "TicketStatus");

			Sql(@"UPDATE [dbo].[Settings] SET [SettingValue] = '2.0.2' WHERE [SettingName] = 'Version'");
		}


	}
}
