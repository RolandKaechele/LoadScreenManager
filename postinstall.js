// postinstall.js
// Creates required folders under ../Assets if they do not already exist,
// and optionally copies example files on first install.

const fs = require('fs');
const path = require('path');
const readline = require('readline');

const assetsDir   = path.resolve(__dirname, '../');
const examplesDir = path.resolve(__dirname, 'Examples');

const folders = [
  'Resources/LoadScreens',   // bundled LoadScreenDefinition JSON files (loaded via Resources.Load)
  'LoadScreens',             // external / mod definitions (loaded from persistentDataPath)
  'Sprites/LoadScreens'      // background sprites for load screens
];

// Create folders if they do not exist
folders.forEach(folder => {
  const fullPath = path.join(assetsDir, folder);
  if (!fs.existsSync(fullPath)) {
    fs.mkdirSync(fullPath, { recursive: true });
    console.log(`Created folder: ${fullPath}`);
  } else {
    console.log(`Folder already exists: ${fullPath}`);
  }
});

// Helper to copy a file with overwrite prompt
function copyFileWithPrompt(src, dest, rl, cb) {
  if (fs.existsSync(dest)) {
    rl.question(`File ${dest} exists. Overwrite? (y/N): `, answer => {
      if (answer.trim().toLowerCase() === 'y') {
        fs.copyFileSync(src, dest);
        console.log(`Overwritten: ${dest}`);
      } else {
        console.log(`Skipped: ${dest}`);
      }
      cb();
    });
  } else {
    fs.copyFileSync(src, dest);
    console.log(`Copied: ${dest}`);
    cb();
  }
}

// Recursively find all files in a directory
function getAllFiles(dir, files = []) {
  if (!fs.existsSync(dir)) return files;
  fs.readdirSync(dir).forEach(entry => {
    const full = path.join(dir, entry);
    if (fs.statSync(full).isDirectory()) getAllFiles(full, files);
    else files.push(full);
  });
  return files;
}

// Copy example files that belong in Resources/
const exampleFiles = getAllFiles(examplesDir);
if (exampleFiles.length === 0) {
  console.log('No example files to copy.');
  process.exit(0);
}

const rl = readline.createInterface({ input: process.stdin, output: process.stdout });

function copyNext(index) {
  if (index >= exampleFiles.length) {
    rl.close();
    console.log('LoadScreenManager post-install complete.');
    return;
  }
  const src  = exampleFiles[index];
  const rel  = path.relative(examplesDir, src);
  const dest = path.join(assetsDir, rel);
  const dir  = path.dirname(dest);
  if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
  copyFileWithPrompt(src, dest, rl, () => copyNext(index + 1));
}

copyNext(0);
