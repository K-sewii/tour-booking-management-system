# Photo quality update

Updated on 2026-11-19 after the project owner selected AI detail restoration.

- 28 destination photos and the sign-in city photo were restored with the built-in OpenAI image generation tool. The reference composition and landmark were retained, while fine texture was reconstructed. These are AI-restored illustrations based on real source photographs, not untouched camera originals; small lettering and other fine details may differ.
- Destination files remain WebP at quality 92. The sign-in photo remains PNG. Existing image URLs are unchanged, so no database update is required.
- The Bãi Trước banner uses a real 3840 × 2560 Wikimedia photo. Attribution and license are recorded in [banners/README.md](banners/README.md).
- Chợ Bà Rịa uses the previously verified 4400 × 2200 source photograph instead of the small multi-market collage.
- Bình Châu uses the Springs Pool photo from the [official Minera website](https://minera.vn/kham-pha/). Chùa Bà Thiên Hậu uses the previously verified Bình Dương temple reference.

[image-quality-manifest.json](image-quality-manifest.json) records the method, source references, prompts, original and final dimensions, file sizes, and hashes. Reference links identify the source or subject; they do not grant new image rights. Original versions are retained in Git history and in the local ignored `artifacts/image-quality-audit/originals` backup.

The SVG icons and illustrations were left as vectors. Image quality changes do not alter tour booking or payment behavior.
