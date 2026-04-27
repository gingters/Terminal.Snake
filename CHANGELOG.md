# Changelog

## 1.0.0 (2026-04-27)


### Features

* **domain:** cell, direction, snake, and board value types ([451d784](https://github.com/gingters/Terminal.Snake/commit/451d78484ec818cf428d5356c5c8395723d30703))
* **game:** arrow keys jump the selection to the nearest snake in direction ([7d63b61](https://github.com/gingters/Terminal.Snake/commit/7d63b6135dbe89738aa3ab897425377716789fab))
* **game:** arrow keys jump the selection to the nearest snake in direction ([bd87453](https://github.com/gingters/Terminal.Snake/commit/bd8745301aa51925c134801b1a73ad2d53775480)), closes [#19](https://github.com/gingters/Terminal.Snake/issues/19)
* **game:** demo starts 1s after an explicit D, not after the idle timeout ([70b79b3](https://github.com/gingters/Terminal.Snake/commit/70b79b3161feaa3eb0d36e7c0bbc90e76ade7cc1))
* **game:** digit keys jump to a specific level ([21210e1](https://github.com/gingters/Terminal.Snake/commit/21210e1ffc2cf47952ddb0742ecbc3d45a1be037))
* **game:** digit keys jump to a specific level so players can skip the intro ([9794a21](https://github.com/gingters/Terminal.Snake/commit/9794a2113df0133d5a68db9ed96b8e37a41890c4)), closes [#25](https://github.com/gingters/Terminal.Snake/issues/25)
* **game:** dynamic board sizing and a multi-digit level-jump prompt ([69c443f](https://github.com/gingters/Terminal.Snake/commit/69c443f2ce57a0408c81b990b8fc32bc8ab85af6)), closes [#36](https://github.com/gingters/Terminal.Snake/issues/36)
* **game:** dynamic board sizing and multi-digit level prompt ([c86222e](https://github.com/gingters/Terminal.Snake/commit/c86222e16a494a48dc472618a37b61dff64ae50e))
* **game:** engine, level manager, idle watcher, and wiring ([a3c5114](https://github.com/gingters/Terminal.Snake/commit/a3c51144e89d4f87ba892f696cfff952ecc902e7))
* **game:** half-plane fallback keeps arrow selection usable in sparse layouts ([11568a3](https://github.com/gingters/Terminal.Snake/commit/11568a3e35ff460157deca2ad294c4a8d20c7b16)), closes [#19](https://github.com/gingters/Terminal.Snake/issues/19)
* **game:** keep auto-play running across level transitions ([91721b7](https://github.com/gingters/Terminal.Snake/commit/91721b73e65cef7e447b33340c59cf96ed1e349e))
* **game:** narrow-to-wide funnel for arrow key snake selection ([f70f275](https://github.com/gingters/Terminal.Snake/commit/f70f27507c1625eb9c1d7b998c48ac85a4c89bbf))
* **game:** only auto-play before the user takes over ([0e2d1ee](https://github.com/gingters/Terminal.Snake/commit/0e2d1ee6d4f0d055ffacd03c2df939cd800e274d))
* **game:** only auto-play before the user takes over ([c4a8da2](https://github.com/gingters/Terminal.Snake/commit/c4a8da202ad5481422b0503285afa7832e9a2906)), closes [#16](https://github.com/gingters/Terminal.Snake/issues/16)
* **game:** stop the enter-spam path to finishing a level ([ee09f7f](https://github.com/gingters/Terminal.Snake/commit/ee09f7ff7672a67c5da208d1686e12d78e2edc3e))
* **game:** stop the enter-spam path to finishing a level ([02f7faa](https://github.com/gingters/Terminal.Snake/commit/02f7faaf01af3e049b234f1a898288316b2a4be5)), closes [#14](https://github.com/gingters/Terminal.Snake/issues/14)
* **generation:** breadth-first solver over board states ([1d1f5d7](https://github.com/gingters/Terminal.Snake/commit/1d1f5d78fd68564f9fc277e6c8262a04b9cff65f))
* **generation:** constructive placement delivers dense, guaranteed-solvable boards ([ebb67c4](https://github.com/gingters/Terminal.Snake/commit/ebb67c4293953236fdbbee1a37a4550917f183f2))
* **generation:** denser snake packing so high levels actually feel hard ([d7fddee](https://github.com/gingters/Terminal.Snake/commit/d7fddee31abb6303cc58715260ae1353b6b3f52e))
* **generation:** fill the full terminal by level 5 and pack more snakes ([93e74f3](https://github.com/gingters/Terminal.Snake/commit/93e74f3fbabda16ac9abc13e4ffdfda6bddcb7d3))
* **generation:** mix starter + middle snakes for a proper ordering puzzle ([05686fe](https://github.com/gingters/Terminal.Snake/commit/05686fe1acf86bceb307f66fdc1996b6d85a378c))
* **generation:** partial-move solver + background level prefetch ([e5a0036](https://github.com/gingters/Terminal.Snake/commit/e5a0036904d89a40c48e41bf6191d0b56d6a4a82))
* **generation:** partial-move solver + background level prefetch (first pass) ([ebe342f](https://github.com/gingters/Terminal.Snake/commit/ebe342ffd44ce5365d7c570dc25a96d1b1a40e41))
* **generation:** procedural generator with solver validation ([0887a61](https://github.com/gingters/Terminal.Snake/commit/0887a6158c321ee24878cbf64728de330cbf60e5))
* **generation:** scale snake count and length with the actual board size ([6388d5f](https://github.com/gingters/Terminal.Snake/commit/6388d5f2915d0462c7b7ce1ff0a18bb61a0b136d))
* **generation:** scale snake length so later levels have long snakes ([b90da75](https://github.com/gingters/Terminal.Snake/commit/b90da756043499bf95b1c08d27ba69dff767c0d1))
* **generation:** scale snake length so later levels have long snakes ([b0c4f39](https://github.com/gingters/Terminal.Snake/commit/b0c4f397dd14b3811c1fe480cc2520d81dfaebbe)), closes [#13](https://github.com/gingters/Terminal.Snake/issues/13)
* **generation:** widen starter head-zone so snake rays actually cross ([c1d83b6](https://github.com/gingters/Terminal.Snake/commit/c1d83b6d42362f9a64120ccfa56711c2005e1236))
* **hud:** add localized help legend with H toggle and press-H hint ([5ea5089](https://github.com/gingters/Terminal.Snake/commit/5ea508996748397cd069baa595d9f44d572f7b68))
* **hud:** add localized help legend with H toggle and press-H hint ([3b6db1b](https://github.com/gingters/Terminal.Snake/commit/3b6db1b87fa32a34eb072913888aa852c01b3ae4)), closes [#15](https://github.com/gingters/Terminal.Snake/issues/15)
* **input:** clear the terminal when the app exits ([9559715](https://github.com/gingters/Terminal.Snake/commit/9559715b9f3da817287328a985e53c853bd16337))
* **input:** clear the terminal when the app exits ([709e6c1](https://github.com/gingters/Terminal.Snake/commit/709e6c1445a325d50999bd6da518f3d6fd6ee74b)), closes [#22](https://github.com/gingters/Terminal.Snake/issues/22)
* **input:** cross-platform decoder and buffered stream parser ([f7dc579](https://github.com/gingters/Terminal.Snake/commit/f7dc57998ede0b6a6f9c58524440dfbbfc7de836))
* **levels:** ten curated tutorial boards ([aa52cbd](https://github.com/gingters/Terminal.Snake/commit/aa52cbd9f4c3dd961c19109c4cd062082b2e9749))
* **movement:** advance engine with blocking and exit sequence ([249ce2e](https://github.com/gingters/Terminal.Snake/commit/249ce2eec104850d4b410b9eadb49cf1e86d9045))
* **rendering:** alternate body shades so snake segments are distinguishable ([78e215b](https://github.com/gingters/Terminal.Snake/commit/78e215b335e67d4c6b1d89429f9cba35d30a63af))
* **rendering:** frame the play area with a visible board border ([bca4438](https://github.com/gingters/Terminal.Snake/commit/bca4438ac07d6d8e0dd079144cf4e33b02618736))
* **rendering:** frame the play area with a visible board border ([c22229e](https://github.com/gingters/Terminal.Snake/commit/c22229ee09769b1fa0bd3d939160556a4ce3d0e1)), closes [#24](https://github.com/gingters/Terminal.Snake/issues/24)
* **rendering:** render the snake body as a double-line pipe ([fef47f7](https://github.com/gingters/Terminal.Snake/commit/fef47f7f2468e0db4e0a75f1de28662b2e62f5b0)), closes [#27](https://github.com/gingters/Terminal.Snake/issues/27)
* **rendering:** spectre live board view and animation scheduler ([9eba234](https://github.com/gingters/Terminal.Snake/commit/9eba234b668e170e1b3696d52a29d7ab13e261ec))
* **rendering:** viewport calculator, frame buffer, board renderer ([6df2dec](https://github.com/gingters/Terminal.Snake/commit/6df2dec4603c69d6f87da55d6e557eaca0a6ef25))


### Bug Fixes

* **game:** rebuild viewport after Tick, not before it ([79584dd](https://github.com/gingters/Terminal.Snake/commit/79584ddc690689f9e33974846759839ef82d0455))
* **game:** rebuild viewport on every render so level-ups do not crash ([1511a68](https://github.com/gingters/Terminal.Snake/commit/1511a68c5d7220269dcc763f0b895c3c41d426d9))
* **game:** rebuild viewport on every render so level-ups do not crash ([de83f84](https://github.com/gingters/Terminal.Snake/commit/de83f841609ced5435ba5ad3213d3d1955b926c7)), closes [#7](https://github.com/gingters/Terminal.Snake/issues/7)
* **game:** suppress selection highlight during snake exit animation ([2273825](https://github.com/gingters/Terminal.Snake/commit/22738258826e9bc9776047b3466303d95f54b762))
* **game:** suppress selection highlight during snake exit animation ([1f5ae5f](https://github.com/gingters/Terminal.Snake/commit/1f5ae5f8985d31c0a9ada52d757f11dac2938b92)), closes [#29](https://github.com/gingters/Terminal.Snake/issues/29)
* **game:** survive the final frames of a snake's exit animation ([2e88af6](https://github.com/gingters/Terminal.Snake/commit/2e88af666190c4c55920b3acceaf3b4fb657ed04))
* **game:** survive the final frames of a snake's exit animation ([9b6a486](https://github.com/gingters/Terminal.Snake/commit/9b6a4869cd6a2f57c019ffd481911417944f7514)), closes [#3](https://github.com/gingters/Terminal.Snake/issues/3)
* **input:** add Linux raw-mode termios wrapper ([e2a6cad](https://github.com/gingters/Terminal.Snake/commit/e2a6cad8fae554e64808854af68f3e0bae6ed5ae))
* **input:** add Linux raw-mode termios wrapper ([37131db](https://github.com/gingters/Terminal.Snake/commit/37131dbff10410d8694ba2deaef9958f43912992))
* **input:** bypass .NET Console line reader and read /dev/tty directly ([055bb0c](https://github.com/gingters/Terminal.Snake/commit/055bb0ccb25a746bfb078e3addaef5cf48030838))
* **input:** decode SS3 arrow sequences instead of quitting the app ([8ee0310](https://github.com/gingters/Terminal.Snake/commit/8ee0310edee1f3121ecc1f5e3f1f814926615204))
* **input:** decode SS3 arrow sequences instead of quitting the app ([32ab515](https://github.com/gingters/Terminal.Snake/commit/32ab51542659f521f1aa705e82186a541e60001a)), closes [#18](https://github.com/gingters/Terminal.Snake/issues/18)
* **input:** enable raw + VT mode on Windows console and pin UTF-8 output ([beff825](https://github.com/gingters/Terminal.Snake/commit/beff825801e57f03d0bcf11bd1aaa8ce9da3911d))
* **input:** enable raw + VT mode on Windows console and pin UTF-8 output ([7757182](https://github.com/gingters/Terminal.Snake/commit/775718213feee2431b07998aea9a904b382f8dd1))
* **input:** put the tty in raw mode so keystrokes reach the engine ([929abb0](https://github.com/gingters/Terminal.Snake/commit/929abb04869ccea1ea6af642b1afde167763f0ac))
* **input:** put the tty in raw mode so keystrokes reach the engine ([414ac84](https://github.com/gingters/Terminal.Snake/commit/414ac846a2dbd20d0a6adefeabfaeebd79233f78)), closes [#9](https://github.com/gingters/Terminal.Snake/issues/9)
* **input:** switch macOS termios P/Invoke to LibraryImport with explicit libSystem path ([28440ea](https://github.com/gingters/Terminal.Snake/commit/28440ea33a41f52d50f00a674b786fbc7d32c0d9))
* **input:** switch raw-mode to direct tcgetattr/tcsetattr ([a062931](https://github.com/gingters/Terminal.Snake/commit/a06293152ba656eae73ed6918d364a6fc46b81d7))
* **input:** switch the raw-mode implementation to tcgetattr/tcsetattr ([bb13ae4](https://github.com/gingters/Terminal.Snake/commit/bb13ae48b0d2c923cc30ccdb51bef40b94ae0009)), closes [#11](https://github.com/gingters/Terminal.Snake/issues/11)
* **rendering:** keep the selection visible on terminals without reverse video ([9966379](https://github.com/gingters/Terminal.Snake/commit/99663794524bd03f3b6cf79827a78961b5d646ed))
* **rendering:** make the selected snake visibly highlighted ([ca51acf](https://github.com/gingters/Terminal.Snake/commit/ca51acf8f4ec799963ff0ab1fec0b8721bdb810f))
* **rendering:** make the selected snake visibly highlighted ([daeb815](https://github.com/gingters/Terminal.Snake/commit/daeb815a7256108e737efe250d1acd0ea033b737)), closes [#1](https://github.com/gingters/Terminal.Snake/issues/1)
* **rendering:** stop the playing field from jumping on redraw ([c25acbd](https://github.com/gingters/Terminal.Snake/commit/c25acbd7dd779ededb11f2f1ade070cde5df3668))
* **rendering:** stop the playing field from jumping on redraw ([39c720c](https://github.com/gingters/Terminal.Snake/commit/39c720c4b8ada37c16bfa6d656063753aa54bfb9)), closes [#2](https://github.com/gingters/Terminal.Snake/issues/2)
* **rendering:** suppress the trailing line break in BoardView ([b7b3b8a](https://github.com/gingters/Terminal.Snake/commit/b7b3b8afb6f675ebc2e304fe18ef17d118aab8bd))
* **test-env:** guard empty arrays under bash 3 nounset ([c5144ea](https://github.com/gingters/Terminal.Snake/commit/c5144ea41b76ed490be5a1b01cd4ee64b3151041))
* **test-env:** silence FromPlatformFlagConstDisallowed BuildKit lint ([1849e14](https://github.com/gingters/Terminal.Snake/commit/1849e146e648854734c220e6147859dfa011ec7a))


### Performance

* **generation:** sub-10ms level generation with a BenchmarkDotNet suite ([986b029](https://github.com/gingters/Terminal.Snake/commit/986b0299e4c9b59e8f7af3831ffbc0055437cceb))


### Refactoring

* **game:** split DispatchGameplayKey into grouped handlers for complexity ([7418642](https://github.com/gingters/Terminal.Snake/commit/7418642cec403a436afdb0b85361c083797e1206))
* split hot methods to fit the complexity and crap gate ([eec7396](https://github.com/gingters/Terminal.Snake/commit/eec739634b9d20b5fe8632e12ed6bc17c2c0d106))


### Documentation

* finalize readme with architecture overview ([2a6dac6](https://github.com/gingters/Terminal.Snake/commit/2a6dac62eef642d56fc37410e29dba08ffad5e7a))
