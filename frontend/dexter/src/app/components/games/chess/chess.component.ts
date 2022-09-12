import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, HostListener, Input, OnChanges, OnInit, SimpleChanges, ViewChild } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { HubConnection } from '@microsoft/signalr';
import { ToastrService } from 'ngx-toastr';
import { EmptyError, firstValueFrom, lastValueFrom, Subject } from 'rxjs';
import { clearSelection, timeout } from 'src/app/classes/helpers';
import { GameConnection } from 'src/app/models/GameConnection';
import { GameProfile } from 'src/app/models/GameProfile';
import { GameRoom } from 'src/app/models/GameRoom';
import { ApiService } from 'src/app/services/api.service';
import { ConnectionManagerService } from 'src/app/services/connection-manager.service';
import { GameConnectionManagerService } from 'src/app/services/game-connection-manager.service';
import { GamesApiService } from 'src/app/services/games-api.service';
import { IGame } from '../game-display/IGame';
import * as Chess from './chess';
import { underAttack } from './chess';
import { JoinGameDialogComponent } from './dialogs/join-game-dialog/join-game-dialog.component';
import { Capture } from './models/Capture';
import { GameEndContext } from './models/GameEndContext';
import { CHECK, createMove, Move, PMove, promotionSection } from './models/Move';
import { MoveUpdateDto } from './models/MoveUpdateDto';
import { Premove } from './models/Premove';

@Component({
  selector: 'app-chess',
  templateUrl: './chess.component.html',
  styleUrls: ['./chess.component.css']
})
export class ChessComponent implements OnInit, AfterViewInit, OnChanges, IGame {

  constructor(private toastr: ToastrService, private api: ApiService, private gamesApi: GamesApiService,
    private change: ChangeDetectorRef, private dialog: MatDialog, private hub: ConnectionManagerService) { }

  @Input() connection: GameConnection | undefined;
  @Input() game: GameRoom | undefined;
  @Input() gameHook!: Subject<GameRoom>;

  @ViewChild("area")  areaElement!: ElementRef<HTMLElement>;
  @ViewChild("board") boardElement: ElementRef | undefined;

  board = "";
  data?: ChessData;
  preferences = {
    theme: "default",
    tileWhite: "#dadfc9",
    tileBlack: "#2a2a42",
    highlightOpacity: 0.65
  }
  inverted = true;

  heldPiece?: Chess.Piece;
  pieces: Chess.Piece[] = [];
  heldOffsetsInitial = {x: 0, y: 0};
  heldOffsets = {x: 0, y: 0};
  legalMoves: number[] = [];
  potentialPremoves: number[] = [];

  gameActive = false;

  moves: PMove[] = [];
  get lastMove() {return this.moves[this.moves.length - 1]};
  captures: Capture[] = [];

  profileWhite: GameProfile | undefined;
  profileBlack: GameProfile | undefined;

  TIMER_INTERVAL = 100;
  checkPlayersOnTimer = false;
  checkJoinSideOnTimer = false;
  ngOnInit(): void {
    this.hub.getConnection("/games").then(c => {
      this.setupEvents(c);
    })
    setInterval(() => this.timerCallback(), this.TIMER_INTERVAL);
  }

  ngAfterViewInit(): void {
    if (this.game == null) return;
    this.data = JSON.parse(this.game.data, (key, value) => {
      if (key == "board") {
        return (value as string).split("");
      }
      return value;
    });
    this.calcSqSize(this.areaElement.nativeElement);

    if (this.connection) {
      this.connectionStablished(this.connection);
    }
    this.updatePlayers(this.game);
  }

  ngOnChanges(changes: SimpleChanges) {
    console.log(`changes present with ` + JSON.stringify(changes))
    if (changes['game']) {
      this.gameOnRoomChanged(changes['game'].currentValue);
    }
    if (changes['connection'] && this.connection) {
      this.gameOnConnectionChanged(this.connection);
    }
  }
  gameOnRoomChanged(room?: GameRoom): void {
    if (room) this.refreshGame(room);
    this.checkPlayersOnTimer = true;
  }
  gameOnConnectionChanged(connection?: GameConnection): void {
    if (connection) this.connectionStablished(connection);
  }

  setupEvents(c: HubConnection) {
    console.log("Settings up chess events");
    this.gameHook.subscribe(game => this.updatePlayers(game));
    c.on("refresh", (room: GameRoom) => {
      //this.change.detach();
      this.refreshGame(room);
      this.change.detectChanges();
      //this.change.reattach();
    })
    c.on("move", (move: any) => {
      console.log("Received move event with value = " + JSON.stringify(move))
      this.processMove(new PMove({value: move.value}));
      if (this.data) {
        this.data.timerPaused = false;
        this.data.blackTime = move.timerBlack;
        this.data.whiteTime = move.timerWhite;
        if (move.gameEnds) {
          this.completeGame(move.gameEndContext!);
        }
      }
    })
    c.on("joinSide", async (playerId: string, side: string) => {
      await this.processJoinSide(playerId, side.toLowerCase() == "white");
      if (this.game) this.updatePlayers(this.game);
    })
    c.on("leaveSide", async (playerId: string) => {
      while (!this.data) await timeout(500);
      if (this.data.blackPlayer == playerId) this.data.blackPlayer = "0";
      if (this.data.whitePlayer == playerId) this.data.whitePlayer = "0";
      if (this.game) this.updatePlayers(this.game);
    })
  }

  refreshGame(room: GameRoom) {
    this.game = room;
    this.data = JSON.parse(room.data, (key, value) => {
      if (key == "board") {
        return (value as string).split("");
      }
      return value;
    })
    this.updatePlayers(room)

    if (this.data!.lastMove) this.moves = [new PMove({value: this.data!.lastMove})]
    this.pieces = [];
    this.check = ((this.data?.lastMove ?? 0) & CHECK) > 0;
    for (let [i,p] of this.data?.board.entries() ?? []) {
      if (p == ' ') continue;
      let piece = Chess.Piece.fromChar(p, i);
      if (piece != null)
        this.pieces.push(piece);
    }

    if ((this.data!.blackPlayer == "0" || this.data!.whitePlayer == "0")
      && this.connection && this.data!.blackPlayer != this.connection.userId && this.data!.whitePlayer != this.connection.userId) {
      this.requestJoinSide();
    }
  }

  processMove(move: PMove) {
    console.log("Processing move " + JSON.stringify(move))
    move.check = false;
    if (this.data)
      this.data.lastMove = createMove(move.origin, move.target, move.capture, false, move.promotesTo).value;

    const piece = this.pieces.find(p => p.pos == move.origin);
    if (piece && this.data) {
      if (piece == this.heldPiece) {
        this.heldPiece = undefined;
      }
      let captured = piece.executeMove(this.data, this.pieces, move, false);
      if (captured) {
        this.captures.push({pos: captured.pos, piece: captured.char})
      }

      const kingpos = !this.data.whiteToMove ? this.data.whiteKing : this.data.blackKing;
      if (underAttack(this.data, this.pieces, kingpos, this.data.whiteToMove)) {
        move.check = true;
        this.data.lastMove! |= CHECK;
      }
    }

    this.moves.push(move);
    if (this.data) {
      if (this.data.whiteToMove) {
        this.data.whiteTime += this.data.increment;
      } else {
        this.data.blackTime += this.data.increment;
      }
    }

    this.postMoveRecalc(move);
  }

  undoMove(): PMove | undefined {
    let move = this.moves.pop();
    if (!move) return;
    let piece = this.pieces.find(p => p.pos == move!.target);
    if (piece) {
      piece.setPos(move.origin, this.data);
      if (move.capture) {
        const cp = this.captures.pop();
        if (cp) {
          const p = Chess.Piece.fromChar(cp.piece, cp.pos);
          if (p) this.pieces.push(p);
        }
      }
      if ((move.promotesTo ?? ' ') != ' ') {
        let index = this.pieces.indexOf(piece);
        this.pieces[index] = new Chess.Pawn(piece.isWhite ? 'P' : 'p', move.origin);
      }

      // KING MOVES
      if (piece instanceof Chess.King) {
        if (this.data) {
          if (piece.isWhite) {
            this.data.whiteKing = move.origin;
          } else {
            this.data.blackKing = move.origin;
          }
        }

        // CASTLING
        if (Math.abs(move.origin - move.target) == 2) {
          let rook = this.pieces.find(p => p.pos == (move!.origin + move!.target) / 2)
          if (rook) {
            const newpos = move.target > move.origin ? rook.rank * 8 + 7 : rook.rank * 8;
            rook.setPos(newpos);
          }
        }
      }
    }

    this.postMoveRecalc(move);
    if (this.data) {
      this.data.halfMoves--
      if (this.data.halfMoves < 0) this.data.halfMoves = 0;
      if (this.data.whiteToMove) this.data.currentMove--;
    }
    return move;
  }

  check = false;
  postMoveRecalc(move?: PMove) {
    if (!this.data) return;
    this.check = move?.check ?? false;

    this.data.timerPaused = !Boolean(move);
    this.data.whiteToMove = !this.data.whiteToMove;
  }

  timerCallback() {
    if (this.checkPlayersOnTimer) {
      if (this.game) {
        this.checkPlayersOnTimer = false;
        this.updatePlayers(this.game);
      }
    }
    if (this.checkJoinSideOnTimer) {
      this.checkJoinSideOnTimer = false;
      this.requestJoinSide()
    }

    if (this.data === undefined) return;
    if (this.data.timerPaused) return;

    let newval;
    if (this.data.whiteToMove) {
      this.data.whiteTime -= this.TIMER_INTERVAL;
      newval = this.data.whiteTime;
    } else {
      this.data.blackTime -= this.TIMER_INTERVAL;
      newval = this.data.blackTime;
    }

    if (newval >= 0) return;

    this.completeGame({
      gameResult: (this.data.whiteToMove ? 0 : 1),
      reason: "Timer ran out",
      eloBalance: [NaN, NaN]
    });
  }

  onPieceClicked(piece: Chess.Piece, event: MouseEvent) {
    event.preventDefault();
    clearSelection();

    this.heldOffsetsInitial = {x: event.screenX, y: event.screenY}
    this.heldOffsets = {x: 0, y: 0}

    if (!this.data || !this.connection) return;
    if ( piece.isWhite && this.connection.userId != this.data.whitePlayer) return;
    if (!piece.isWhite && this.connection.userId != this.data.blackPlayer) return;
    if (!this.gameActive) return;

    this.heldPiece = piece;

    this.legalMoves = [];
    this.potentialPremoves = [];
    if (piece.isWhite == this.data.whiteToMove)
      this.legalMoves = piece.getLegalMoves(this.data, this.pieces);
    else
      this.potentialPremoves = piece.getPotentialMoves(this.data, this.pieces);
  }

  @HostListener('mousemove', ['$event']) onMouseMove(event: MouseEvent) {
    if (this.heldPiece) {
      //console.log("Processing mouse move");
      this.heldOffsets = {
        x: event.screenX - this.heldOffsetsInitial.x,
        y: event.screenY - this.heldOffsetsInitial.y
      }
    }
    event.preventDefault();
  }

  @HostListener('mouseup', ['$event']) onMouseUp(event: MouseEvent) {
    if (!this.heldPiece) return;
    let sq = this.getCalcSquareSize();

    let fileOffset = Math.round(this.heldOffsets.x / sq);
    let rankOffset = Math.round(this.heldOffsets.y / sq);
    if (this.inverted) {
      fileOffset *= -1;
      rankOffset *= -1;
    }
    let oldFile = this.heldPiece.file;
    let oldRank = this.heldPiece.rank;
    if (this.heldPiece.premoveState.affected) {
      oldFile = Chess.file(this.heldPiece.premoveState.position);
      oldRank = Chess.rank(this.heldPiece.premoveState.position);
    }
    let newFile = oldFile + fileOffset;
    let newRank = oldRank + rankOffset;

    if (newFile < 0 || newFile > 7 || newRank < 0 || newRank > 7) {
      this.heldPiece = undefined; return;
    }

    let newpos = newRank * 8 + newFile;
    this.pieceDropped(this.heldPiece, newpos);

    this.legalMoves = [];
    this.potentialPremoves = [];
  }

  async pieceDropped(piece: Chess.Piece, newpos: number): Promise<void> {
    console.log(`Dropped ${piece.char} from ${piece.pos} at ${newpos}`);
    if (!this.data) {
      this.heldPiece = undefined; return;
    }

    if (this.data.whiteToMove != piece.isWhite) {
      this.registerPremove(piece, newpos);
      return;
    }

    this.tryPushMove(piece, newpos);
  }

  async tryPushMove(piece: Chess.Piece, newpos: number, promotion = ' ') {
    if (!this.data) {return;}

    if (!piece.canMove(this.data, this.pieces, newpos)) {
      this.cancelMove(); return;
    }

    let move = piece.getMove(this.data, this.pieces, newpos);
    if ((move.value & 0x0f000000) == 0x0f000000) {
      if (promotion == ' ') {
        promotion = await this.requestPromotionType(piece.isWhite, newpos);
      }
      move.value &= ~0x0f000000;
      move.value |= promotionSection(promotion);
    }

    if (this.game && this.connection) {
      this.gamesApi.request(this.connection, this.game.id, "move", move)
    }
  }

  cancelMove() {
    this.heldPiece = undefined;
    this.premoves = [];
    this.recalcPremoves();
  }

  premoves: Premove[] = [];
  premoveMarkers: number[] = [];
  async registerPremove(piece: Chess.Piece, target: number): Promise<void> {
    if (!this.data) return;

    let allowed = piece.getPotentialMoves(this.data, this.pieces);
    if (!allowed.includes(target)) {
      this.heldPiece = undefined;
      return;
    }

    let move = {piece: piece, target: target, promoteTo: ' '}
    if ((piece.isWhite ? target < 8 : target >= (this.data?.board.length ?? 64) - 8) && piece instanceof Chess.Pawn) {
      let p = await this.requestPromotionType(piece.isWhite, target);
      move.promoteTo = p;
    }
    this.premoves.push(move);
    this.recalcPremoves();

    this.heldPiece = undefined;
  }

  recalcPremoves() {
    for (let piece of this.pieces) {
      piece.premoveState.affected = false;
      this.premoveMarkers = [];
    }
    for (let pm of this.premoves) {
      let targetPiece = this.pieces.find(p => p.premoveState.affected ? (p.premoveState.position == pm.target) : (p.pos == pm.target));
      pm.piece.premoveState.affected = true;
      pm.piece.premoveState.position = pm.target;
      if (targetPiece) {
        targetPiece.premoveState.affected = true;
        targetPiece.premoveState.position = -1; // Captured
      }
      if (!this.premoveMarkers.includes(pm.piece.pos))
        this.premoveMarkers.push(pm.piece.pos);
      if (!this.premoveMarkers.includes(pm.target))
        this.premoveMarkers.push(pm.target);
    }
  }

  startGame() {
    if (this.data)
      this.data.timerPaused = false;
  }

  async connectionStablished(connection: GameConnection): Promise<void> {
    while (!this.data)
      await timeout(500);

    if (connection.userId == this.data.whitePlayer)
      this.inverted = false;
    if (connection.userId == this.data.blackPlayer)
      this.inverted = true;

    if (this.data.whitePlayer != "0" && this.data.whitePlayer != "0") return;
    if (this.data.whitePlayer == connection.userId || this.data.blackPlayer == connection.userId) return;

    this.requestJoinSide();
    this.checkPlayersOnTimer = true;
  }

  updatePlayers(game: GameRoom) {
    if (!this.data) {
      this.checkPlayersOnTimer = true;
      return;
    }
    let shouldUpdateWhite = !(this.profileWhite && this.profileWhite.id == this.data.whitePlayer) && this.data.whitePlayer != "0";
    let shouldUpdateBlack = !(this.profileBlack && this.profileBlack.id == this.data.blackPlayer) && this.data.blackPlayer != "0";
    this.gameActive = this.data.blackPlayer != "0" && this.data.whitePlayer != "0";

    if (shouldUpdateWhite) {
      console.log("Reviewing player list for white player")
      this.profileWhite = game.players.find(p => p.id == this.data!.whitePlayer)
      if (this.connection && this.connection.userId == this.data.whitePlayer)
        this.inverted = false;
      if (this.profileWhite == undefined) { this.checkPlayersOnTimer = true; }
    }
    if (shouldUpdateBlack) {
      console.log("Reviewing player list for black player")
      this.profileBlack = game.players.find(p => p.id == this.data!.blackPlayer)
      if (this.connection && this.connection.userId == this.data.blackPlayer)
        this.inverted = true;
      if (this.profileBlack == undefined) { this.checkPlayersOnTimer = true; }
    }
  }

  dialogActive = false;
  async requestJoinSide() : Promise<string | undefined> {
    if (this.dialogActive) return;
    if (!this.data || !this.game || !this.connection || this.connection.userId == "" || this.connection.userId == "0") {
      this.checkJoinSideOnTimer = true;
      return;
    }
    if (this.data.whitePlayer == this.connection.userId || this.data.blackPlayer == this.connection.userId) return;
    if (this.data.whitePlayer != "0" && this.data.blackPlayer != "0") return;

    console.log(`Requesting Join with ID ${this.connection.userId}. WHITE = ${this.data.whitePlayer}; BLACK = ${this.data.blackPlayer}`)

    this.dialogActive = true;
    const ref = this.dialog.open(JoinGameDialogComponent, {
      width: '450px',
      data: this.data
    });

    let result = await lastValueFrom(ref.afterClosed())
    if (!result || result == "" || !this.game || !this.connection) return;

    try {
      await this.gamesApi.request(this.connection, this.game.id, "join", result);
    } catch {
      this.toastr.error("Couldn't join side " + result + "!");
      return "";
    }
    this.dialogActive = false;
    return result;
  }

  async processJoinSide(playerId: string, white: boolean): Promise<void> {
    while (!this.data) {
      await timeout(500);
    }
    console.log(`${playerId} joined side ${white ? 'white' : 'black'}`);

    if (white) {
      this.data.whitePlayer = playerId;
    } else {
      this.data.blackPlayer = playerId;
    }
  }

  completeGame(context: GameEndContext) {
    if (this.data) {
      this.data.timerPaused = true;
      if (this.data.blackTime < 0) this.data.blackTime = 0;
      if (this.data.whiteTime < 0) this.data.whiteTime = 0;
    }
  }

  requestingPromotion = -1;
  promotables: string[] = ["Q", "R", "B", "N"]
  promotionResponse = new Subject<string>();
  async requestPromotionType(white: boolean, position: number): Promise<string> {
    let held = this.heldPiece;
    let oldpos = held?.renderPos;
    this.heldPiece = undefined;

    this.promotables = white ? ["Q", "R", "B", "N"] : ["q", "r", "b", "n"];
    this.requestingPromotion = position;

    let result;
    try {
      console.log("Awaiting promotion response");
      result = (await firstValueFrom(this.promotionResponse.asObservable())).toUpperCase();
      console.log("Awaited promotion response successfully");
    } catch {
      console.log("Failed to await promotion response");
      result = "Q";
    }

    this.requestingPromotion = -1;
    this.heldPiece = held;
    if (oldpos && held) held.renderPos = oldpos;
    return result;
  }

  isAltTile(i: number) {
    return ((i / 8) & 1) != i % 2;
  }

  index(i: number) {
    return this.inverted ? 63 - i : i;
  }

  getPieceStyle(p: Chess.Piece) {
    const held = p == this.heldPiece;
    let file;
    let rank;

    if (p.premoveState.affected) {
      let pos = p.premoveState.position;
      if (pos < 0) return {'display': 'none'};
      file = Chess.file(pos);
      rank = Chess.rank(pos);
      if (this.inverted) {
        file = 7 - file;
        rank = 7 - rank;
      }
    }
    else
    {
      file = this.inverted ? 7 - p.file : p.file;
      rank = this.inverted ? 7 - p.rank : p.rank;
    }

    return {
      'transform': `translate(` +
        `calc(var(--sq-size) * ${file} + ${held ? this.heldOffsets.x : '0'}px),` +
        `calc(var(--sq-size) * ${rank} + ${held ? this.heldOffsets.y : '0'}px))`,
      'transition': `all ${held ? '0.03s' : '0.15s'} linear`
    }
  }
  getPromoteStyle(pos: number) {
    let file = this.inverted ? 7 - Chess.file(pos) : Chess.file(pos);
    let rank = this.inverted ? 7 - Chess.rank(pos) : Chess.rank(pos);
    return {
      'transform': `translate(` +
        `calc(var(--sq-size) * ${file}),` +
        `calc(var(--sq-size) * ${rank})` +
      `)`
    }
  }

  getPieceAssetName(p: Chess.Piece) {
    if (p.isWhite)
      return "W" + p.char.toUpperCase();
    else
      return "B" + p.char.toUpperCase();
  }
  getPieceCharAssetName(p: string) {
    if (p == p.toUpperCase()) {
      return "W" + p.toUpperCase();
    } else {
      return "B" + p.toUpperCase();
    }
  }

  @HostListener('window:resize', ['$event'])
  onResize(event : Event) {
    this.calcSqSize(this.areaElement.nativeElement);
  }

  sqSize: string = "64px";
  calcSqSize(element: HTMLElement) {
    console.log("Recalculating square size from bounds");
    let width = element.clientWidth;
    let height = element.clientHeight;
    height -= 308; // Timers + Margin
    width -= 116; // Margins
    let size = Math.min(height, width) / 8;
    this.sqSize = `${size}px`;
    this.change.detectChanges();
  }

  getCalcSquareSize(): number {
    if (!this.boardElement) return 0;

    return this.boardElement.nativeElement.clientWidth / 8;
  }
}

export interface ChessData {
  board: string[];
  castling: number;
  enPassant: number;
  whiteToMove: boolean;
  currentMove: number;
  halfMoves: number;
  whiteKing: number;
  blackKing: number;
  lastMove?: number;

  lastTime: number;
  timerPaused: boolean;
  increment: number;
  whiteTime: number;
  blackTime: number;

  whitePlayer: string;
  blackPlayer: string;
  casual : boolean;
}
