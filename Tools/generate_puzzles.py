"""Offline SudokuIQ pack generator. Python 3, standard library only.
Difficulty is the strongest technique used by a deterministic logical solver.
No guessing is accepted. Every exported puzzle is checked for uniqueness.
"""
import argparse, json, random
from pathlib import Path
CLASSIC_UNITS = [[r*9+c for c in range(9)] for r in range(9)] + [[r*9+c for r in range(9)] for c in range(9)] + [[(br+r)*9+bc+c for r in range(3) for c in range(3)] for br in (0,3,6) for bc in (0,3,6)]
def configure(variant):
    global UNITS, PEERS
    if variant not in ('classic','sudoku-x'): raise ValueError('Unknown variant')
    UNITS = CLASSIC_UNITS + ([[i*10 for i in range(9)], [8+i*8 for i in range(9)]] if variant=='sudoku-x' else [])
    PEERS = [set().union(*(set(u) for u in UNITS if i in u))-{i} for i in range(81)]
configure('classic')
DIGITS = set(range(1,10))
def candidates(a,i): return DIGITS-{a[j] for j in PEERS[i]}
def count_solutions(a, limit=2):
    empty = [(len(c:=candidates(a,i)),i,c) for i in range(81) if not a[i]]
    if not empty: return 1
    _,i,c = min(empty,key=lambda x:x[0]); total=0
    for n in c:
        a[i]=n; total += count_solutions(a,limit-total); a[i]=0
        if total>=limit: break
    return total

def rate(a):
    a=a[:]; marks={i:candidates(a,i) for i in range(81) if not a[i]}; rank=0
    while marks:
        if any(not c for c in marks.values()): return None
        singles=[(i,next(iter(c))) for i,c in marks.items() if len(c)==1]
        if not singles:
            for u in UNITS:
                for n in range(1,10):
                    places=[i for i in u if i in marks and n in marks[i]]
                    if len(places)==1: singles=[(places[0],n)]; rank=max(rank,1); break
                if singles: break
        if singles:
            i,n=singles[0]; a[i]=n; del marks[i]
            for j in PEERS[i]:
                if j in marks: marks[j].discard(n)
            continue
        changed=False
        # Locked candidates: all candidates for a digit in one unit also lie in another.
        for u in UNITS:
            for n in range(1,10):
                places={i for i in u if i in marks and n in marks[i]}
                if len(places)<2: continue
                for v in UNITS:
                    if u==v or not places.issubset(v): continue
                    for j in set(v)-set(u):
                        if j in marks and n in marks[j]: marks[j].remove(n); changed=True
        # Naked pairs eliminate both candidates from all other cells of their unit.
        for u in UNITS:
            pairs={tuple(sorted(marks[i])) for i in u if i in marks and len(marks[i])==2}
            for pair in pairs:
                cells=[i for i in u if i in marks and marks[i]==set(pair)]
                if len(cells)!=2: continue
                for j in u:
                    if j in marks and j not in cells and marks[j].intersection(pair):
                        marks[j].difference_update(pair); changed=True
        if not changed: return None
        rank=2
    return rank

def complete(rng):
    a=[0]*81
    def fill():
        empty=[(len(c:=candidates(a,i)),i,list(c)) for i in range(81) if not a[i]]
        if not empty: return True
        _,i,c=min(empty,key=lambda x:x[0]); rng.shuffle(c)
        for n in c:
            a[i]=n
            if fill(): return True
        a[i]=0; return False
    fill(); return a

def generate(each,seed,prefix,variant="classic"):
    configure(variant)
    rng=random.Random(seed); groups=[[],[],[]]; seen=set()
    for attempt in range(3000):
        solution=complete(rng); a=solution[:]; order=list(range(81)); rng.shuffle(order)
        for i in order:
            old=a[i]; a[i]=0
            if count_solutions(a[:])!=1: a[i]=old; continue
            if sum(bool(x) for x in a)>40: continue
            rank=rate(a)
            key=''.join(map(str,a))
            # At most one puzzle per complete board, to avoid near-duplicate puzzles.
            if rank is not None and len(groups[rank])<each and key not in seen:
                name=['Beginner','Intermediate','Advanced'][rank]
                groups[rank].append(dict(id=f'{prefix}-{name.lower()}-{len(groups[rank])+1:03}',variant=variant,difficulty=name,givens=key,solution=''.join(map(str,solution)),technique=['Naked singles','Hidden singles','Locked candidates / naked pairs'][rank]))
                seen.add(key); print(f'{name}: {len(groups[rank])}/{each}',flush=True); break
        if all(len(g)==each for g in groups): return dict(schemaVersion=1,puzzles=sum(groups,[]))
    raise RuntimeError('Generation limit reached; try another seed.')

if __name__=='__main__':
    ap=argparse.ArgumentParser(); ap.add_argument('--variant',choices=['classic','sudoku-x','both'],default='both'); ap.add_argument('--each',type=int,default=2); ap.add_argument('--seed',type=int,default=260926); ap.add_argument('--prefix',default='starter'); ap.add_argument('--output',default=str(Path(__file__).resolve().parents[1]/'Assets/Resources/sudokuiq-puzzles.json')); args=ap.parse_args()
    if not 1<=args.each<=30: ap.error('--each must be 1–30')
    if args.variant=='both':
        if args.each>16: ap.error('--each must be at most 16 when generating both variants')
        pack=generate(args.each,args.seed,args.prefix,'classic')
        pack['puzzles']+=generate(args.each,args.seed+1,args.prefix+'-x','sudoku-x')['puzzles']
    else:
        pack=generate(args.each,args.seed,args.prefix,args.variant)
    Path(args.output).write_text(json.dumps(pack,indent=2)+'\n',encoding='utf-8'); print('Saved',args.output)
