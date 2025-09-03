#!/usr/bin/env python3
"""
Script to create test PDF and DOCX files from markdown templates
Requires: pip install python-docx reportlab
"""

import os
import subprocess
from pathlib import Path

def create_pdf_from_markdown(md_file, pdf_file):
    """Convert markdown to PDF using pandoc if available, otherwise create simple PDF"""
    try:
        # Try using pandoc if installed
        result = subprocess.run([
            'pandoc', md_file, 
            '-o', pdf_file,
            '--pdf-engine=xelatex',
            '-V', 'geometry:margin=1in',
            '-V', 'mainfont:DejaVu Sans',
            '-V', 'fontsize:12pt'
        ], capture_output=True, text=True, timeout=30)
        
        if result.returncode == 0:
            print(f"Created PDF using pandoc: {pdf_file}")
            return True
    except (FileNotFoundError, subprocess.TimeoutExpired):
        pass
    
    # Fallback: create simple text PDF
    try:
        from reportlab.lib.pagesizes import letter
        from reportlab.pdfgen import canvas
        
        with open(md_file, 'r', encoding='utf-8') as f:
            content = f.read()
        
        c = canvas.Canvas(pdf_file, pagesize=letter)
        c.setFont("Helvetica", 10)
        
        y = 750
        for line in content.split('\n'):
            if y < 50:
                c.showPage()
                c.setFont("Helvetica", 10)
                y = 750
            c.drawString(50, y, line[:100])  # Limit line length
            y -= 15
        
        c.save()
        print(f"Created simple PDF: {pdf_file}")
        return True
        
    except ImportError:
        print(f"Could not create PDF - missing reportlab library")
        return False

def create_docx_from_markdown(md_file, docx_file):
    """Convert markdown to DOCX using pandoc if available, otherwise create simple DOCX"""
    try:
        # Try using pandoc if installed
        result = subprocess.run([
            'pandoc', md_file, 
            '-o', docx_file
        ], capture_output=True, text=True, timeout=30)
        
        if result.returncode == 0:
            print(f"Created DOCX using pandoc: {docx_file}")
            return True
    except (FileNotFoundError, subprocess.TimeoutExpired):
        pass
    
    # Fallback: create simple text DOCX
    try:
        from docx import Document
        
        with open(md_file, 'r', encoding='utf-8') as f:
            content = f.read()
        
        doc = Document()
        for line in content.split('\n'):
            if line.strip() and not line.startswith('#') and len(line) > 3:
                doc.add_paragraph(line)
        
        doc.save(docx_file)
        print(f"Created simple DOCX: {docx_file}")
        return True
        
    except ImportError:
        print(f"Could not create DOCX - missing python-docx library")
        return False

def create_large_file(file_path, target_size_mb):
    """Create a large file for testing size limits"""
    target_size = target_size_mb * 1024 * 1024
    content = "This is a test document for size limit testing. " * 1000
    
    with open(file_path, 'w') as f:
        while os.path.getsize(file_path) < target_size:
            f.write(content)
    
    print(f"Created large file: {file_path} ({os.path.getsize(file_path)/1024/1024:.1f}MB)")

def main():
    test_dir = Path(__file__).parent
    
    # Create test documents
    test_files = [
        ('sample-contract.md', 'sample-contract.pdf', 'sample-contract.docx'),
        ('sample-lease.md', 'sample-lease.pdf', 'sample-lease.docx'),
    ]
    
    for md_name, pdf_name, docx_name in test_files:
        md_path = test_dir / md_name
        pdf_path = test_dir / pdf_name
        docx_path = test_dir / docx_name
        
        if md_path.exists():
            create_pdf_from_markdown(str(md_path), str(pdf_path))
            create_docx_from_markdown(str(md_path), str(docx_path))
    
    # Create large test file (接近10MB)
    large_pdf_path = test_dir / "large-document.pdf"
    create_large_file(str(large_pdf_path), 9.5)
    
    # Create oversized file (>10MB)
    oversized_pdf_path = test_dir / "oversized-file.pdf"
    create_large_file(str(oversized_pdf_path), 11)
    
    # Create invalid file type
    invalid_path = test_dir / "invalid-file.txt"
    with open(invalid_path, 'w') as f:
        f.write("This is a plain text file - should be rejected by upload system.")
    print(f"Created invalid file: {invalid_path}")
    
    print("\nTest files created successfully!")
    print("Files available for testing:")
    for file in test_dir.iterdir():
        if file.is_file() and file.suffix in ['.pdf', '.docx', '.txt']:
            size_mb = file.stat().st_size / (1024 * 1024)
            print(f"  {file.name} ({size_mb:.1f}MB)")

if __name__ == "__main__":
    main()