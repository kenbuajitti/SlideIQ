"""Offline MineIQ generator. Python 3, standard library only.
Accepts boards only when visible clues logically reveal every safe tile.
Difficulty tiers use board size/density, not a calibrated human rating.
"""
import argparse, json, random
from pathlib import Path

def neighbours(n,i):
    x,y=i%n,i//n
    return {yy*n+xx for yy in range(max(0,y-1),min(n,y+2)) for xx in range(max(0,x-1),min(n,x+2)) if yy*n+xx!=i}

def solve(p):
    n=p['size']; mines={i for i,c in enumerate(p['mines']) if c=='1'}; allcells=set(range(n*n))
    adj=[neighbours(n,i) for i in range(n*n)]; counts=[len(a&mines) for a in adj]
    opened=set(); flagged=set(); rounds=0
    def flood(indices):
        todo=list(indices)
        while todo:
            i=todo.pop()
            assert i not in mines, 'Solver made an unsafe deduction'
            if i in opened:continue
            opened.add(i)
            if counts[i]==0:todo.extend(adj[i]-opened)
    flood([p['start']]); initial=len(opened)
    while len(opened)<n*n-len(mines):
        rounds+=1
        constraints=[]
        for i in opened:
            hidden=adj[i]-opened-flagged
            if hidden:constraints.append((hidden,counts[i]-len(adj[i]&flagged)))
        constraints.append((allcells-opened-flagged,len(mines)-len(flagged)))
        if p['variant']=='survey':
            for k in range(n):
                for group in ({k*n+x for x in range(n)},{y*n+k for y in range(n)}):
                    hidden=group-opened-flagged
                    if hidden:constraints.append((hidden,len(group&mines)-len(group&flagged)))
        safe=set(); bombs=set()
        for group,remaining in constraints:
            assert 0<=remaining<=len(group)
            if remaining==0:safe|=group
            elif remaining==len(group):bombs|=group
        if not safe and not bombs:
            # Subset deduction compares only constraints derived from visible clues.
            for a,ca in constraints:
                for b,cb in constraints:
                    if a<b:
                        diff=b-a; remaining=cb-ca
                        if remaining==0:safe|=diff
                        elif remaining==len(diff):bombs|=diff
        if not safe and not bombs:return False,rounds,initial
        assert bombs<=mines
        flagged|=bombs;flood(safe)
    return True,rounds,initial

def generate(seed=260926,per_group=10):
    rng=random.Random(seed);puzzles=[];seen=set()
    for variant in ('classic','survey'):
        for difficulty,n,m in [('Beginner',6,6),('Intermediate',8,12),('Advanced',10,22)]:
            accepted=0
            for attempt in range(100000):
                start=rng.randrange(n*n); protected=neighbours(n,start)|{start}
                mines=set(rng.sample(sorted(set(range(n*n))-protected),m))
                layout=''.join('1' if i in mines else '0' for i in range(n*n))
                if layout in seen:continue
                p=dict(id=f'{variant}-{difficulty.lower()}-{accepted+1:02}',variant=variant,difficulty=difficulty,size=n,start=start,mines=layout,technique='Adjacent counts and subset deductions'+('; row/column totals' if variant=='survey' else ''))
                ok,rounds,initial=solve(p)
                if not ok or rounds<2 or initial>n*n*.6:continue
                puzzles.append(p);seen.add(layout);accepted+=1
                if accepted==per_group:break
            if accepted!=per_group:raise RuntimeError('Could not generate enough boards')
    return dict(schemaVersion=1,puzzles=puzzles)

if __name__=='__main__':
    ap=argparse.ArgumentParser();ap.add_argument('--output',default=str(Path(__file__).resolve().parents[1]/'Assets/Resources/mineiq-puzzles.json'));ap.add_argument('--seed',type=int,default=260926);ap.add_argument('--per-group',type=int,default=10);ap.add_argument('--verify');args=ap.parse_args()
    if args.verify:
        pack=json.loads(Path(args.verify).read_text());assert all(solve(p)[0] for p in pack['puzzles']);print(f"PASS: {len(pack['puzzles'])} boards solved without guesses.")
    else:
        pack=generate(args.seed,args.per_group);Path(args.output).write_text(json.dumps(pack,indent=2)+'\n');print(f"Generated {len(pack['puzzles'])} verified boards: {args.output}")
