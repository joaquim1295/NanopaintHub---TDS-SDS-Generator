import sys

path = r'INFRASTRUCTURE/NanopaintHub.Infrastructure.LocalData/Persistence/DatabaseSeeder.cs'
with open(path, 'rb') as f:
    data = f.read()

# Replace typical UTF-8 mojibake patterns
replacements = {
    b'\xc3\x82\xc2\xae': b'\xc2\xae',          # Â® -> ®
    b'\xc3\xa2\xe2\x82\xac\xe2\x80\x9c': b'\xe2\x80\x93',  # â€“ -> –
    b'\xc3\x82\xc2\xb5': b'\xc2\xb5',          # Âµ -> µ
    b'\xc3\x82\xc2\xb0': b'\xc2\xb0',          # Â° -> °
    b'\xc3\x83\xc2\xa2': b'\xc3\xa2',          # Ã¢ -> â (e.g. in Substâncias)
    b'\xc3\x83\xc2\xad': b'\xc3\xad',          # Ã­ -> í
    b'\xc3\x83\xc2\xa3': b'\xc3\xa3',          # Ã£ -> ã
    b'\xc3\x83\xc2\xa1': b'\xc3\xa1',          # Ã¡ -> á
    b'\xc3\x83\xc2\xa7': b'\xc3\xa7',          # Ã§ -> ç
    b'\xc3\x83\xc2\xb3': b'\xc3\xb3',          # Ã³ -> ó
    b'\xc3\x83\xc2\xaa': b'\xc3\xaa',          # Ãª -> ê
    b'\xc3\x83\xc2\xa9': b'\xc3\xa9',          # Ã© -> é
    b'\xc3\x83\xc2\xba': b'\xc3\xba',          # Ãº -> ú
    b'\xc3\x83\xc2\x82': b'\xc3\x82',          # Ã‚ -> Â
}

for bad, good in replacements.items():
    data = data.replace(bad, good)

with open(path, 'wb') as f:
    f.write(data)
