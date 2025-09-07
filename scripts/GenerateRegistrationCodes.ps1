#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generate registration codes for the Better Call Saul application
.DESCRIPTION
    This script generates secure registration codes that can be used for user registration.
    It connects to the application database and inserts the codes.
.PARAMETER Count
    Number of codes to generate (default: 1)
.PARAMETER ExpireDays
    Number of days until the codes expire (default: 30)
.PARAMETER CreatedBy
    Who is creating the codes (default: 'Admin')
.PARAMETER Notes
    Optional notes for the codes
.PARAMETER ConnectionString
    Database connection string (defaults to development database)
.PARAMETER List
    List existing registration codes
.PARAMETER Cleanup
    Remove expired and used registration codes
.EXAMPLE
    ./GenerateRegistrationCodes.ps1 -Count 5 -ExpireDays 60
.EXAMPLE
    ./GenerateRegistrationCodes.ps1 -List
.EXAMPLE
    ./GenerateRegistrationCodes.ps1 -Cleanup
#>

param(
    [int]$Count = 1,
    [int]$ExpireDays = 30,
    [string]$CreatedBy = "Admin",
    [string]$Notes = "",
    [string]$ConnectionString = "",
    [switch]$List,
    [switch]$Cleanup
)

# Function to generate secure codes
function New-SecureCode {
    param([int]$Length = 12)
    
    $chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
    $code = ""
    
    for ($i = 0; $i -lt $Length; $i++) {
        $randomIndex = Get-Random -Minimum 0 -Maximum $chars.Length
        $code += $chars[$randomIndex]
    }
    
    return $code
}

# Set default connection string if not provided
if ([string]::IsNullOrEmpty($ConnectionString)) {
    $ConnectionString = "Server=(localdb)\mssqllocaldb;Database=BetterCallSaulDb;Trusted_Connection=true;TrustServerCertificate=true;"
}

try {
    # Load SQL Server assemblies
    Add-Type -AssemblyName "System.Data"
    
    # Create connection
    $connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
    $connection.Open()
    
    Write-Host "Connected to database successfully." -ForegroundColor Green
    
    if ($List) {
        # List existing codes
        $query = @"
            SELECT Code, CreatedBy, CreatedAt, ExpiresAt, IsUsed, Notes 
            FROM RegistrationCodes 
            ORDER BY CreatedAt DESC
"@
        
        $command = New-Object System.Data.SqlClient.SqlCommand($query, $connection)
        $reader = $command.ExecuteReader()
        
        Write-Host "Existing Registration Codes:" -ForegroundColor Yellow
        Write-Host ("-" * 100) -ForegroundColor Gray
        Write-Host ("{0,-15} {1,-15} {2,-20} {3,-20} {4,-5} {5,-20}" -f "Code", "Created By", "Created", "Expires", "Used", "Notes") -ForegroundColor Cyan
        Write-Host ("-" * 100) -ForegroundColor Gray
        
        while ($reader.Read()) {
            $isUsed = if ($reader["IsUsed"]) { "Yes" } else { "No" }
            Write-Host ("{0,-15} {1,-15} {2,-20} {3,-20} {4,-5} {5,-20}" -f 
                $reader["Code"], 
                $reader["CreatedBy"], 
                $reader["CreatedAt"].ToString("yyyy-MM-dd HH:mm"), 
                $reader["ExpiresAt"].ToString("yyyy-MM-dd HH:mm"), 
                $isUsed,
                $reader["Notes"])
        }
        
        $reader.Close()
        return
    }
    
    if ($Cleanup) {
        # Remove expired and used codes
        $query = "DELETE FROM RegistrationCodes WHERE ExpiresAt < GETUTCDATE() OR IsUsed = 1"
        $command = New-Object System.Data.SqlClient.SqlCommand($query, $connection)
        $deletedCount = $command.ExecuteNonQuery()
        
        Write-Host "Cleaned up $deletedCount expired or used registration codes." -ForegroundColor Green
        return
    }
    
    # Generate new codes
    $expirationDate = (Get-Date).AddDays($ExpireDays).ToUniversalTime()
    $generatedCodes = @()
    
    Write-Host "Generating $Count registration codes..." -ForegroundColor Yellow
    Write-Host "Created by: $CreatedBy" -ForegroundColor Gray
    Write-Host "Expires: $($expirationDate.ToString('yyyy-MM-dd HH:mm')) UTC" -ForegroundColor Gray
    if (![string]::IsNullOrEmpty($Notes)) {
        Write-Host "Notes: $Notes" -ForegroundColor Gray
    }
    Write-Host ""
    
    for ($i = 0; $i -lt $Count; $i++) {
        do {
            $code = New-SecureCode
            
            # Check if code already exists
            $checkQuery = "SELECT COUNT(*) FROM RegistrationCodes WHERE Code = @Code"
            $checkCommand = New-Object System.Data.SqlClient.SqlCommand($checkQuery, $connection)
            $checkCommand.Parameters.AddWithValue("@Code", $code) | Out-Null
            $exists = $checkCommand.ExecuteScalar() -gt 0
        } while ($exists)
        
        # Insert the code
        $insertQuery = @"
            INSERT INTO RegistrationCodes (Id, Code, CreatedBy, IsUsed, ExpiresAt, CreatedAt, Notes)
            VALUES (@Id, @Code, @CreatedBy, 0, @ExpiresAt, @CreatedAt, @Notes)
"@
        
        $insertCommand = New-Object System.Data.SqlClient.SqlCommand($insertQuery, $connection)
        $insertCommand.Parameters.AddWithValue("@Id", [System.Guid]::NewGuid()) | Out-Null
        $insertCommand.Parameters.AddWithValue("@Code", $code) | Out-Null
        $insertCommand.Parameters.AddWithValue("@CreatedBy", $CreatedBy) | Out-Null
        $insertCommand.Parameters.AddWithValue("@ExpiresAt", $expirationDate) | Out-Null
        $insertCommand.Parameters.AddWithValue("@CreatedAt", (Get-Date).ToUniversalTime()) | Out-Null
        $insertCommand.Parameters.AddWithValue("@Notes", $Notes) | Out-Null
        
        $insertCommand.ExecuteNonQuery() | Out-Null
        $generatedCodes += $code
    }
    
    Write-Host "Generated codes:" -ForegroundColor Green
    Write-Host ("-" * 50) -ForegroundColor Gray
    foreach ($code in $generatedCodes) {
        Write-Host $code -ForegroundColor White
    }
    Write-Host ("-" * 50) -ForegroundColor Gray
    Write-Host "Successfully generated $($generatedCodes.Count) registration codes!" -ForegroundColor Green
}
catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
finally {
    if ($connection -and $connection.State -eq "Open") {
        $connection.Close()
    }
}