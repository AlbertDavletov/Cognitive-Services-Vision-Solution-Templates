import React from 'react'
import { View, Image, Text, TouchableOpacity, FlatList } from 'react-native'
import { NavigationScreenProp, NavigationState, NavigationParams } from 'react-navigation'
import { SearchBar } from 'react-native-elements'
import { styles } from './TagCollectionScreen.style'

interface TagCollectionScreenProps {
    navigation: NavigationScreenProp<NavigationState, NavigationParams>;
}

interface TagCollectionScreenState {
    filter: string;
    allTags: Array<any>;
    filterTags: Array<any>;
}

export class TagCollectionScreen extends React.Component<TagCollectionScreenProps, TagCollectionScreenState> {
    constructor(props: TagCollectionScreenProps) {
        super(props);
        this.state = {
            filter: '',
            allTags: [],
            filterTags: []
        };
    }

    componentDidMount() {
        const { navigation, } = this.props;
        let tags = navigation.getParam('tags', []);
        this.setState({
            allTags: tags, 
            filterTags: tags 
        });
    }

    render() {
        const { filter } = this.state;
        
        return (
            <View style={styles.mainContainer}>
                <SearchBar
                    inputContainerStyle={styles.searchInputContainerStyle}
                    containerStyle={styles.searchContainerStyle}
                    placeholder="Search"
                    onChangeText={this.updateSearchFilter}
                    value={filter}
                />
                
                <FlatList 
                    numColumns={4}
                    data={ this.state.filterTags } 
                    renderItem={({item}) =>
                        <TouchableOpacity activeOpacity={0.6} style={styles.tagBlockStyle} 
                            onPress={() => this.selectTag(item)}>
                            <View>
                                <View style={{ alignSelf: 'center', height: '60%', padding: 6 }}>
                                    { item.name.toLocaleLowerCase() != 'product' && item.name.toLocaleLowerCase() != 'gap' &&
                                        <Image style={styles.imageThumbnail} source={{ uri: item.imageUrl }} />
                                    }
                                    { item.name.toLocaleLowerCase() == 'product' &&
                                        <Image style={styles.imageThumbnail} source={require('../../assets/product.jpg')} />
                                    }
                                    { item.name.toLocaleLowerCase() == 'gap' &&
                                        <Image style={styles.imageThumbnail} source={require('../../assets/gap.jpg')} />
                                    }
                                </View>
                                
                                <View style={[styles.tagContainer, { height: '40%' }]}>
                                    <Text numberOfLines={3} style={styles.tagLabel}>{item.name}</Text>
                                </View>
                            </View>
                        </TouchableOpacity>
                    }
                />

            </View>
        );
    }

    selectTag(tag: any) {
        const { navigation } = this.props;
        if (navigation.state?.params?.returnData) {
            navigation.state.params.returnData(tag);
            navigation.goBack();
        }
    }

    updateSearchFilter = (filter: string) => {
        let allTags = this.state.allTags;
        
        if (!filter) {
            this.setState({ filterTags: allTags });
        } else {
            let filterData = Array<any>();
            allTags.forEach(t => {
                let tagName = t.name.toLocaleLowerCase();
                if (tagName.includes(filter.toLocaleLowerCase())) {
                    filterData.push(t);
                }
            });
            this.setState({ filterTags: filterData });
        }
        this.setState({ filter });
    }
}
