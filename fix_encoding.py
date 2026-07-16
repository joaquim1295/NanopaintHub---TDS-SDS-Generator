import sys
path = r'INFRASTRUCTURE/NanopaintHub.Infrastructure.LocalData/Persistence/DatabaseSeeder.cs'
with open(path, 'r', encoding='utf-8') as f:
    text = f.read()

text = text.replace('Â®', '®')
text = text.replace('â€“', '–')
text = text.replace('Âµ', 'µ')
text = text.replace('Â°', '°')

with open(path, 'w', encoding='utf-8') as f:
    f.write(text)
