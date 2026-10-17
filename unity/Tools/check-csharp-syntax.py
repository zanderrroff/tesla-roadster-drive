#!/usr/bin/env python3
"""Requires tree-sitter 0.26.0 and tree-sitter-c-sharp 0.23.5. Syntax only."""
import pathlib,sys
from tree_sitter import Language,Parser
import tree_sitter_c_sharp
parser=Parser(Language(tree_sitter_c_sharp.language()));root=pathlib.Path(__file__).resolve().parents[1]
errors=[];files=list((root/'MotorCoast/Assets').rglob('*.cs'))
for p in files:
    source=p.read_bytes();tree=parser.parse(source)
    def walk(n):
        if n.type=='ERROR' or n.is_missing:errors.append(f'{p.relative_to(root)}:{n.start_point.row+1}: {n.type}: {source[n.start_byte:n.end_byte][:100]!r}')
        for c in n.children:walk(c)
    walk(tree.root_node)
if errors:print('\n'.join(errors));sys.exit(1)
print(f'C# grammar PASS: {len(files)} files. Unity API binding/compilation NOT checked by this parser.')
