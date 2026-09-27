"""Independent bit-mask solver checks both variants against stored solutions."""
import json
from collections import Counter
from pathlib import Path
from generate_puzzles import rate, configure

def solve(text,variant='classic'):
    a=list(map(int,text)); used=[0]*29; solutions=[]; members=[]
    for i in range(81):
        r,c=divmod(i,9);groups=[r,9+c,18+r//3*3+c//3]
        if variant=='sudoku-x':
            if r==c:groups.append(27)
            if r+c==8:groups.append(28)
        members.append(groups)
    for i,n in enumerate(a):
        if n:
            bit=1<<n
            for g in members[i]: assert not used[g]&bit;used[g]|=bit
    def walk():
        best=None
        for i,n in enumerate(a):
            if n:continue
            m=1022
            for g in members[i]:m&=~used[g]
            if not m:return
            if best is None or m.bit_count()<best[0]:best=(m.bit_count(),i,m)
        if best is None:solutions.append(''.join(map(str,a)));return
        _,i,m=best
        while m:
            bit=m&-m;m-=bit;a[i]=bit.bit_length()-1
            for g in members[i]:used[g]|=bit
            walk()
            for g in members[i]:used[g]^=bit
            a[i]=0
            if len(solutions)>=2:return
    walk();return solutions

if __name__=='__main__':
    root=Path(__file__).resolve().parents[1]
    pack=json.loads((root/'Assets/Resources/sudokuiq-puzzles.json').read_text())
    assert pack['schemaVersion']==1
    counts=Counter();ids=set()
    for p in pack['puzzles']:
        assert p['id'] not in ids;ids.add(p['id'])
        v=p['variant'];assert v in ('classic','sudoku-x')
        assert len(p['givens'])==len(p['solution'])==81
        assert solve(p['givens'],v)==[p['solution']]
        configure(v)
        assert ['Beginner','Intermediate','Advanced'][rate(list(map(int,p['givens'])))]==p['difficulty']
        if v=='sudoku-x':
            assert set(p['solution'][::10])==set('123456789')
            assert set(p['solution'][8:73:8])==set('123456789')
        counts[v,p['difficulty']]+=1
        print(p['id'],'unique;',sum(n!='0' for n in p['givens']),'clues;',p['technique'])
    assert counts=={(v,d):2 for v in ('classic','sudoku-x') for d in ('Beginner','Intermediate','Advanced')}
    # Same digit separated by row, column and box, but on the same main diagonal.
    a=['0']*81;a[0]=a[40]='5'
    assert len(solve(''.join(a),'classic'))==2
    try:solve(''.join(a),'sudoku-x')
    except AssertionError:pass
    else:raise AssertionError('Diagonal conflict accepted')
    a=['0']*81;a[8]=a[40]='7'
    try:solve(''.join(a),'sudoku-x')
    except AssertionError:pass
    else:raise AssertionError('Anti-diagonal conflict accepted')
    print('PASS: 12 unique solutions, tier counts, logical ratings, both diagonal constraints.')
