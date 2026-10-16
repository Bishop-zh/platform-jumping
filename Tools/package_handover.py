"""Build a GUID-preserving, dependency-classified delivery snapshot; never move project assets."""
from pathlib import Path
import csv, hashlib, html, json, re, shutil, zipfile
from urllib.parse import quote
from collections import Counter

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Deliverables' / '资源交付包'
REPORT = ROOT / 'Artifacts' / 'Handover'
DEP = json.loads((REPORT / 'dependencies.json').read_text(encoding='utf-8-sig'))
USED, REUSABLE = set(DEP['used']), set(DEP['reusable'])
ROWS = []
previous = OUT / '资源索引.json'
if previous.is_file():
    for row in json.loads(previous.read_text(encoding='utf-8')):
        old = (OUT / row['分类'] / row['原始路径']).resolve()
        if not old.is_relative_to(OUT.resolve()):
            raise RuntimeError('Invalid previous package path')
        # Only remove unchanged, previously generated snapshot files. Preserve external edits.
        if old.is_file() and hashlib.sha256(old.read_bytes()).hexdigest() == row['SHA256']:
            old.unlink()
            meta = Path(str(old) + '.meta')
            if meta.is_file():
                meta.unlink()

def copy_asset(path, category, reason):
    src = ROOT / path
    if not src.is_file():
        raise RuntimeError(f'Missing source: {path}')
    dst = OUT / category / path
    dst.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(src, dst)
    meta = Path(str(src) + '.meta')
    if meta.is_file():
        shutil.copy2(meta, Path(str(dst) + '.meta'))
    parent = src.parent
    while parent != ROOT and parent.is_relative_to(ROOT / 'Assets'):
        pm = Path(str(parent) + '.meta')
        if pm.is_file():
            target = OUT / category / pm.relative_to(ROOT)
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(pm, target)
        parent = parent.parent
    digest = hashlib.sha256(src.read_bytes()).hexdigest()
    if hashlib.sha256(dst.read_bytes()).hexdigest() != digest:
        raise RuntimeError(f'Copy mismatch: {path}')
    mt = meta.read_text(encoding='utf-8') if meta.is_file() else ''
    guid = re.search(r'^guid: (.+)$', mt, re.M)
    ppu = re.search(r'spritePixelsToUnits: (.+)', mt)
    ROWS.append({'分类': category, '原始路径': path, '依据': reason,
                 'GUID': guid[1] if guid else '', 'PPU': ppu[1] if ppu else '',
                 '字节': src.stat().st_size, 'SHA256': digest})

resources = []
for prefix in ('Assets/Art', 'Assets/Animations', 'Assets/Prefabs', 'Assets/Audio'):
    for src in sorted((ROOT / prefix).rglob('*')):
        if src.is_file() and src.suffix != '.meta':
            path = src.relative_to(ROOT).as_posix()
            resources.append(path)
            if path in USED:
                copy_asset(path, '01_当前已使用', '启用场景和 Resources 运行配置的递归依赖')
            elif path in REUSABLE:
                copy_asset(path, '02_备用可用组件', '备用 World 预制体及其递归依赖')
            else:
                copy_asset(path, '03_未引用与历史素材', '当前场景及备用 World 预制体均未引用；保留供挑选')

for src in sorted((ROOT / 'ArtSource').rglob('*')):
    if src.is_file():
        copy_asset(src.relative_to(ROOT).as_posix(), '04_原始图集与参考', '原始图集、切片记录、提示词与参考')

for src in sorted((ROOT / 'Assets').rglob('*')):
    if src.is_file() and src.suffix != '.meta':
        path = src.relative_to(ROOT).as_posix()
        if path not in resources:
            copy_asset(path, '00_工程配套', '场景、脚本、配置及其余项目文件')
for prefix in ('Docs', 'Tools', 'ProjectSettings', 'Packages'):
    for src in sorted((ROOT / prefix).rglob('*')):
        if src.is_file():
            copy_asset(src.relative_to(ROOT).as_posix(), '00_工程配套', '交接文档、生成工具与项目设置')
for name in ('navigation-validation.txt', 'dependencies.json', 'runtime-validation.txt', 'ui-preservation.json', 'interaction-validation.txt'):
    copy_asset('Artifacts/Handover/' + name, '00_工程配套', '本轮 Unity 导航检查和资源依赖证据')
for name in ('validation.txt', 'asset-validation.txt'):
    copy_asset('Artifacts/Robot/' + name, '00_工程配套', '机器人专项验证报告')
for name in ('generation.json', 'runtime-validation.txt', 'asset-validation.json'):
    copy_asset('Artifacts/Audio/' + name, '00_工程配套', '音频、首页背景角色和屏幕摄像机验证')

with (OUT / '资源索引.csv').open('w', encoding='utf-8-sig', newline='') as f:
    writer = csv.DictWriter(f, fieldnames=list(ROWS[0]))
    writer.writeheader(); writer.writerows(ROWS)
(OUT / '资源索引.json').write_text(json.dumps(ROWS, ensure_ascii=False, indent=2), encoding='utf-8')
counts = Counter(row['分类'] for row in ROWS)
readme = '''# 项目资源交付包

分类基于 2026-10-07 启用场景和 Resources 运行配置的 Unity AssetDatabase 递归依赖。
01 当前已使用：场景实际引用的美术、动画和预制体。
02 备用可用组件：场景尚未使用，但备用 World 预制体可直接使用的资源。
03 未引用与历史素材：旧图、旧界面和未引用切片；不代表必须删除。
04 原始图集与参考：原图、生成提示词和切片记录。
00 工程配套：脚本、场景、设置、文档和验证报告。

每类保留原始 Assets/... 路径和 .meta。恢复工程时将 00、01、02、03 各自内部的 Assets 文件夹合并到同一个项目根目录，合并 ProjectSettings 与 Packages；请勿将整个分类文件夹拖进 Unity Assets，否则会出现重复 GUID 或错误路径。04 原始素材不需要导入 Unity。请使用 Unity 2022.3.62f3c1，并安装 manifest 中的依赖。

工程内的原资源没有移动；交付包是分类快照。之后手工修改地图或新增资源，应重新生成依赖报告再运行 Tools/package_handover.py 更新交付包。本轮没有覆盖地图。Library、.work、Temp 和用户设置未打包。

从 Docs/手工关卡搭建与交接.md 开始阅读。所有数字继续使用既有数字图片图集。
资源索引包含分类、路径、判定依据、GUID、PPU 与 SHA256 校验值。

文件数（不含 meta）：
'''
readme += '\n'.join(f'- {key}：{value}' for key, value in sorted(counts.items())) + '\n'
(OUT / '请先阅读.md').write_text(readme, encoding='utf-8')
cards = []
for row in ROWS:
    path = row['原始路径']
    if not path.startswith('Assets/Art/') or not path.endswith('.png'):
        continue
    link = quote(row['分类'] + '/' + path, safe='/')
    label = html.escape(row['分类'] + ' / ' + path)
    cards.append(f'<article data-search="{label}"><a href="{link}"><img loading="lazy" src="{link}"></a><p>{label}</p><small>PPU: {row["PPU"] or "UI"}</small></article>')
page = '''<!doctype html><meta charset="utf-8"><title>资源浏览索引</title>
<style>body{background:#19232c;color:#eee;font:14px sans-serif;margin:24px}input{padding:12px;width:70%;margin:16px 0}main{display:grid;grid-template-columns:repeat(auto-fill,minmax(230px,1fr));gap:16px}article{background:#26333e;padding:12px;overflow-wrap:anywhere}img{width:100%;height:160px;object-fit:contain;image-rendering:pixelated;background:repeating-conic-gradient(#333 0% 25%,#444 0% 50%) 0/16px 16px}p{line-height:1.5}small{color:#bfcbd2}</style>
<h1>资源浏览索引</h1><p>输入文件名、分类或路径筛选；点击图片查看原文件。保留工程路径和像素比例，交互功能请按预制体使用。</p>
<input id="filter" placeholder="例如：备用、Column、Robot、Gameplay"><main>'''
page += ''.join(cards) + '''</main><script>document.querySelector('#filter').oninput=e=>{const q=e.target.value.toLowerCase();document.querySelectorAll('article').forEach(a=>a.hidden=!a.dataset.search.toLowerCase().includes(q))}</script>'''
(OUT / '资源浏览索引.html').write_text(page, encoding='utf-8')
archive = OUT.parent / '资源交付包.zip'
with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED) as z:
    for src in sorted(OUT.rglob('*')):
        if src.is_file():
            z.write(src, src.relative_to(OUT.parent))
with zipfile.ZipFile(archive) as z:
    assert z.testzip() is None
result = {'resource_files': len(resources), 'all_files_without_meta': len(ROWS),
          'counts': dict(counts), 'archive_bytes': archive.stat().st_size,
          'checks': ['every resource classified once', 'all copied bytes SHA256 verified', 'ZIP CRC verified', 'original Assets paths and meta preserved']}
(REPORT / 'package-validation.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps(result, ensure_ascii=False))
