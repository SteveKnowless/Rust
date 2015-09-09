import re
import time
import ConVar.Server as sv

DEV = False
LATEST_CFG = 3.0
LINE = '-' * 50
PROFILE = '76561198248442828'

class radline:

    def __init__(self):

        self.Title = 'RAD-Line'
        self.Author = 'SkinN'
        self.Description = 'Turns off radiation for a while from time to time'
        self.Version = V(2, 0, 0)
        self.ResourceId = 914

    # -------------------------------------------------------------------------
    # - CONFIGURATION
    def LoadDefaultConfig(self):
        ''' Hook called when there is no configuration file '''

        self.Config = {
            'CONFIG_VERSION': LATEST_CFG,
            'SETTINGS': {
                'PREFIX': '[ <orange>RAD-Line<end> ]',
                'BROADCAST TO CONSOLE': True,
                'ENABLE PLUGIN ICON': True,
                'NO RAD INTERVAL': 10,
                'RAD INTERVAL': 30
            },
            'MESSAGES': {
                'RAD ON': 'Radiation levels are now up for <orange>{interval} minutes<end>.',
                'RAD OFF': 'Radiation levels are now down for <orange>{interval} minutes<end>.',
                'STATE ON': 'Radiation levels are up for <orange>{remaining} minutes<end>.',
                'STATE OFF': 'Radiation levels are down for <orange>{remaining} minutes<end>.'
            },
            'COLORS': {
                'PREFIX': 'white',
                'MESSAGES': 'grey'
            }
        }

        self.con('* Loading default configuration file')

    # -------------------------------------------------------------------------
    def UpdateConfig(self):
        ''' Function to update the configuration file on plugin Init '''

        if (self.Config['CONFIG_VERSION'] <= LATEST_CFG - 0.2) or DEV:

            self.con('* Configuration version is too old, reseting to default')

            self.Config.clear()

            self.LoadDefaultConfig()

        else:

            self.con('* Applying new changes to configuration file')

            self.Config['CONFIG_VERSION'] = LATEST_CFG

        self.SaveConfig()

    # -------------------------------------------------------------------------
    # - MESSAGE SYSTEM
    def con(self, text, f=False):
        ''' Function to send a server con message '''

        if self.Config['SETTINGS']['BROADCAST TO CONSOLE'] or f:

            print('[%s v%s] :: %s' % (self.Title, str(self.Version), self.format(text, True)))

    # -------------------------------------------------------------------------
    def say(self, text, color='silver', f=True, profile=False):
        ''' Function to send a message to all players '''

        if self.prefix and f:

            rust.BroadcastChat(self.format('%s <%s>%s<end>' % (self.prefix, color, text)), None, PROFILE if not profile else profile)

        else:

            rust.BroadcastChat(self.format('<%s>%s<end>' % (color, text)), None, PROFILE if not profile else profile)

    # -------------------------------------------------------------------------
    def tell(self, player, text, color='silver', f=True, profile=False):
        ''' Function to send a message to a player '''

        if self.prefix and f:

            rust.SendChatMessage(player, self.format('%s <%s>%s<end>' % (self.prefix, color, text)), None, PROFILE if not profile else profile)

        else:

            rust.SendChatMessage(player, self.format('<%s>%s<end>' % (color, text)), None, PROFILE if not profile else profile)

    # -------------------------------------------------------------------------
    # - PLUGIN HOOKS
    def Init(self):

        self.con(LINE)

        if self.Config['CONFIG_VERSION'] < LATEST_CFG or DEV:

            self.UpdateConfig()

        global PLUGIN, MSG, COLOR
        MSG, COLOR, PLUGIN = (self.Config['MESSAGES'], self.Config['COLORS'], self.Config['SETTINGS'])

        self.prefix = '<color=%s>%s</color>' % (self.Config['COLORS']['PREFIX'], PLUGIN['PREFIX']) if PLUGIN['PREFIX'] else None
        self.off_time = PLUGIN['NO RAD INTERVAL'] * 60 if PLUGIN['NO RAD INTERVAL'] else 600
        self.on_time = PLUGIN['RAD INTERVAL'] * 60 if PLUGIN['RAD INTERVAL'] else 1800
        self.int_start = 0

        self.con('* Starting event loop (Interval: %d.%d minute/s)' % divmod(PLUGIN['RAD INTERVAL'] * 60, 60))

        self.loop(True)

        command.AddChatCommand('rad', self.Plugin, 'rad_CMD')
        command.AddChatCommand('radline', self.Plugin, 'plugin_CMD')

        self.con(LINE)

    # -------------------------------------------------------------------------
    # - COMMAND FUNCTIONS
    def rad_CMD(self, player, cmd, args):
        ''' Rad Command Function '''

        if sv.radiation:

            secs = int((PLUGIN['RAD INTERVAL'] * 60) - (time.time() - self.intstamp))

            self.tell(player, MSG['STATE ON'].format(remaining='%d.%d' % divmod(secs, 60)), COLOR['MESSAGES'])

        else:

            secs = int((PLUGIN['NO RAD INTERVAL'] * 60) - (time.time() - self.intstamp))

            self.tell(player, MSG['STATE OFF'].format(remaining='%d.%d' % divmod(secs, 60)), COLOR['MESSAGES'])

    # -------------------------------------------------------------------------
    def plugin_CMD(self, player, cmd, args):
        ''' Plugin command function '''

        if args and args[0] == 'help':

            self.tell(player, '%sCOMMANDS DESCRIPTION:' % ('%s | ' % self.prefix if self.prefix else ''), f=False)
            self.tell(player, LINE, f=False)
            self.tell(player, '<orange>/rad<end> <grey>-<end> Shows the remaining time for the radiation to be On or Off.', f=False)

        else:

            self.tell(player, '<orange><size=18>RAD-Line</size> <grey>v%s<end><end>' % self.Version, profile=PROFILE, f=False)
            self.tell(player, self.Description, profile=PROFILE, f=False)
            self.tell(player, 'Plugin developed by <#9810FF>SkinN<end>, powered by <orange>Oxide 2<end>.', profile='76561197999302614', f=False)


    # -------------------------------------------------------------------------
    # - PLUGIN FUNCTIONS / HOOKS
    def loop(self, force=False):

        if not sv.radiation or force:

            sv.radiation = True

            self.intstamp = time.time()

            timer.Once(self.on_time, self.loop, self.Plugin)

            self.con('- Radiation is now on')

            if not force:

                self.say(MSG['RAD ON'].format(interval=PLUGIN['RAD INTERVAL']), COLOR['MESSAGES'])

        else:

            sv.radiation = False

            self.intstamp = time.time()

            timer.Once(self.off_time, self.loop, self.Plugin)

            self.say(MSG['RAD OFF'].format(interval=PLUGIN['NO RAD INTERVAL']), COLOR['MESSAGES'])

            self.con('- Radiation is now off')

    # -------------------------------------------------------------------------
    def format(self, text, con=False):
        '''
            Replaces color names and RGB hex code into HTML code
        '''

        colors = (
            'red', 'blue', 'green', 'yellow', 'white', 'black', 'cyan',
            'lightblue', 'lime', 'purple', 'darkblue', 'magenta', 'brown',
            'orange', 'olive', 'gray', 'grey', 'silver', 'maroon'
        )

        name = r'\<(\w+)\>'
        hexcode = r'\<(#\w+)\>'
        end = 'end'

        if con:
            for x in (end, name, hexcode):
                for c in re.findall(x, text):
                    if c.startswith('#') or c in colors or x == end:
                        text = text.replace('<%s>' % c, '')
        else:
            text = text.replace('<%s>' % end, '</color>')
            for f in (name, hexcode):
                for c in re.findall(f, text):
                    if c.startswith('#') or c in colors: text = text.replace('<%s>' % c, '<color=%s>' % c)
        return text

    # -------------------------------------------------------------------------
    def SendHelpText(self, player):
        ''' Hook called from HelpText plugin when /help is triggered '''

        self.tell(player, '<orange>/rad<end> <grey>-<end> Shows the remaining time for the radiation to be On or Off.', f=False)

